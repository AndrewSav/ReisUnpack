using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using LzhamWrapper;
using LzhamWrapper.Enums;
using ReisUnpack.DataStructures;

namespace ReisUnpack.Engine
{
    internal class Packer
    {
        private readonly List<FileRegistryEntry> _files = new List<FileRegistryEntry>();
        private readonly ConcurrentDictionary<uint, List<Bucket>> _buckets = new ConcurrentDictionary<uint, List<Bucket>>();
        private readonly MemoryStream _directory = new MemoryStream();
        private readonly int _pathPrefixLength;
        private readonly string _path;
        private readonly FileStream _fs;
        private int _fileCounter;
        private CompressionLevel _level;

        public Packer(string path, FileStream fs, CompressionLevel level)
        {
            _path = path.TrimEnd('\\') + "\\";
            _pathPrefixLength = _path.Length;
            _fs = fs;
            _level = level;
        }

        public class PackerTables
        {
            public int FileCount;
            public byte[] Directory;
            public byte[] FileRegistry;
            public byte[] HashTable;
            public byte[] BucketTable;
        }


        private void RebuildBuckets(uint indexSize)
        {
            ConcurrentDictionary<uint, List<Bucket>> oldBuckets = new ConcurrentDictionary<uint, List<Bucket>>(_buckets);
            _buckets.Clear();
            foreach (List<Bucket> bucketList in oldBuckets.Values)
            {
                foreach (Bucket bucket in bucketList)
                {
                    _buckets.AddOrUpdate(bucket.IndexHash % indexSize, (new[] { bucket }).ToList(), (hash, list) =>
                    {
                        list.Add(bucket);
                        return list;
                    });

                }
            }

        }

        private PackerTables CreatePackResult()
        {
            int indexSize = Util.GetIndexSize((uint)_files.Count);
            RebuildBuckets((uint) indexSize);

            int[] hashTable = new int[indexSize];
            Bucket[] bucktetTable = new Bucket[_files.Count];

            int currentIndex = 0;
            for (int i = 0; i < indexSize; i++)
            {
                if (_buckets.ContainsKey((uint) i))
                {
                    hashTable[i] = currentIndex;
                    foreach (Bucket bucket in _buckets[(uint)i])
                    {
                        bucktetTable[currentIndex++] = bucket;
                    }                    
                }
                else
                {
                    hashTable[i] = -1;
                }
            }

            PackerTables result = new PackerTables
            {
                Directory = _directory.GetBuffer(),
                FileRegistry = Util.StructureArrayToByteArray(_files),
                HashTable = Util.StructureArrayToByteArray(hashTable),
                BucketTable = Util.StructureArrayToByteArray(bucktetTable),
                FileCount = _files.Count
            };

            return result;
        }

        public PackerTables Pack()
        {
            PackFolder(_path);
            return CreatePackResult();
        }

        private ushort PackFolder(string path)
        {
            long pos = _directory.Position;
            DirectoryListingEntryHeader dleh = new DirectoryListingEntryHeader();
            string[] folders = Directory.GetDirectories(path);
            string[] files = Directory.GetFiles(path);
            dleh.numDirectories = (ushort)folders.Length;
            dleh.numFiles = (ushort)files.Length;
            string dlehName = Path.GetFileName(path.Substring(_pathPrefixLength));
            byte[] folderNameBytes = Encoding.UTF8.GetBytes(dlehName);
            ushort headerSize = (ushort)(Marshal.SizeOf(dleh) + folderNameBytes.Length);
            dleh.nameLength = (ushort)(folderNameBytes.Length);
            _directory.Position += headerSize;
            ushort foldersSize = Directory.GetDirectories(path).Aggregate<string, ushort>(0, (current, folder) => (ushort)(current + PackFolder(folder)));

            ushort filesSize = 0;
            foreach (string file in files)
            {
                Console.WriteLine(file);
                string shortName = file.Substring(_pathPrefixLength).Replace("\\","/");
                byte[] nameBytes = Encoding.UTF8.GetBytes(shortName);
                Bucket bucket = new Bucket
                {
                    Index = _fileCounter++,
                    IndexHash = Util.Murmur2(nameBytes, 0),
                    BucketHash = Util.Murmur2(nameBytes, BitConverter.ToUInt32(Encoding.UTF8.GetBytes("AMIT"), 0))
                };
                _buckets.AddOrUpdate(bucket.IndexHash, (new[] {bucket}).ToList(), (hash, list) =>
                {
                    list.Add(bucket);
                    return list;
                });
                uint fileSize = (uint)(new FileInfo(file)).Length;
                FileRegistryEntry fre = new FileRegistryEntry
                {
                    uncompressed = fileSize,
                    fileOffset = (ulong) _fs.Position
                };
                byte[] fileData = File.ReadAllBytes(file);
                CompressionParameters p = new CompressionParameters
                {
                    DictionarySize = Util.GetDictLength(fileSize),
                    UpdateRate = TableUpdateRate.Fastest,
                    Level = _level
                };
                byte[] compressedData = new byte[fileSize];
                int compressedSize = (int)fileSize;
                uint addler = 0;
                CompressStatus status = Lzham.CompressMemory(p, fileData, fileData.Length, 0, compressedData, ref compressedSize, 0, ref addler);
                if (status != CompressStatus.Success && status != CompressStatus.OutputBufferTooSmall)
                {
                    Console.WriteLine($"Error compressing file {file}. ({status})");
                    Environment.FailFast($"Error compressing file {file}. ({status})");
                }
                if (status != CompressStatus.OutputBufferTooSmall)
                {

                    fre.flag = 1;
                    fre.compressed = (uint)compressedSize;
                    _fs.Write(compressedData, 0, compressedSize);
                }
                else
                {
                    fre.flag = 0;
                    fre.compressed = fileSize;
                    using (FileStream f = File.OpenRead(file))
                    {
                        f.CopyTo(_fs);
                    }
                }
                _files.Add(fre);
                string justTheName = Path.GetFileName(file);
                byte[] fileNameBytes = Encoding.UTF8.GetBytes(justTheName);
                _directory.Write(BitConverter.GetBytes((ushort)fileNameBytes.Length), 0, 2);
                _directory.Write(fileNameBytes, 0, fileNameBytes.Length);
                filesSize += (ushort)(fileNameBytes.Length + 2);
            }

            dleh.size = (ushort)(headerSize + foldersSize + filesSize);

            long savePos = _directory.Position;
            _directory.Position = pos;
            byte[] data = Util.StructureToByteArray(dleh);
            _directory.Write(data,0, data.Length);
            _directory.Write(folderNameBytes, 0, folderNameBytes.Length);
            _directory.Position = savePos;

            return dleh.size;
        }
    }
}
