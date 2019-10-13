/*
 BSD License

Copyright (c) 2009, Vladimir Vasiltsov All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
    * Names of its contributors may not be used to endorse or promote products derived from this software without specific prior written permission. 

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE. 
 
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ROOT.Shared.Utils.Archiving.Tar
{
    public class LegacyTarWriter : IDisposable
    {
        private Stream _outStream;
        private readonly bool _ownsStream;
        private readonly string _relativeToPath;
        protected byte[] _buffer = new byte[8192];
        private readonly byte[] _zeroes = Enumerable.Repeat((byte)0, 512).ToArray();
        private bool _isClosed;


        /// <summary>
        /// Writes tar (see GNU tar) archive to a stream
        /// </summary>
        /// <param name="writeStream">stream to write archive to</param>
        /// <param name="ownsStream">Whether or not the writer owns the stream</param>
        public LegacyTarWriter(Stream writeStream, bool ownsStream, string relativeToPath = null)
        {
            _outStream = writeStream;
            _ownsStream = ownsStream;
            _relativeToPath = relativeToPath?.ToLowerInvariant();
        }

        protected virtual Stream OutStream => _outStream;

        public void Dispose()
        {
            Close().Wait();
            if (_ownsStream)
            {
                _outStream.Flush();
                _outStream.Dispose();
                _outStream = null;
            }
        }

        public async Task WriteDirectoryEntryAsync(string path, int userId, int groupId, int mode)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            if (path[path.Length - 1] != Path.DirectorySeparatorChar)
            {
                path += Path.DirectorySeparatorChar;
            }

            var lastWriteTime = Directory.Exists(path) ? Directory.GetLastWriteTime(path) : DateTime.Now;

            await WriteHeaderAsync(path, lastWriteTime, 0, userId, groupId, mode, EntryType.Directory);
        }

        public async Task WriteDirectoryEntryAsync(string path)
        {
            await WriteDirectoryEntryAsync(path, 101, 101, 0777);
        }

        public async Task WriteDirectoryAsync(string directory, bool doRecursive)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentNullException(nameof(directory));

            await WriteDirectoryEntryAsync(directory);


            foreach (var fileName in Directory.GetFiles(directory))
            {
                await WriteAsync(fileName);
            }

            foreach (var dirName in Directory.GetDirectories(directory))
            {
                await WriteDirectoryEntryAsync(dirName);
                await WriteDirectoryAsync(dirName, doRecursive);
            }
        }

        public async Task WriteAsync(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));
            using (FileStream file = File.OpenRead(fileName))
            {
                await WriteAsync(file, file.Length, fileName, 61, 61, 511, File.GetLastWriteTime(file.Name));
            }
        }

        public async Task WriteAsync(FileStream file)
        {
            string path = Path.GetFullPath(file.Name).Replace(Path.GetPathRoot(file.Name), string.Empty);
            path = path.Replace(Path.DirectorySeparatorChar, '/');
            await WriteAsync(file, file.Length, path, 61, 61, 511, File.GetLastWriteTime(file.Name));
        }

        public async Task WriteAsync(Stream data, long dataSizeInBytes, string name)
        {
            await WriteAsync(data, dataSizeInBytes, name, 61, 61, 511, DateTime.Now);
        }

        public virtual async Task WriteAsync(Stream data, long dataSizeInBytes, string name, int userId, int groupId, int mode, DateTime lastModificationTime)
        {
            if (_isClosed)
                throw new TarException("Can not write to the closed writer");
            await WriteHeaderAsync(name, lastModificationTime, dataSizeInBytes, userId, groupId, mode, EntryType.File);
            await CopyStreamAsync(data);
            await AlignTo512Async(dataSizeInBytes, false);
        }

        protected virtual async Task WriteHeaderAsync(string name, DateTime lastModificationTime, long count, int userId, int groupId, int mode, EntryType entryType)
        {
            var modifiedPath = GetPath(name);
            Debug.WriteLine(modifiedPath);
            if (modifiedPath == string.Empty)
            {
                modifiedPath += ".";
            }

            var header = new TarHeader
            {
                FileName = modifiedPath,
                LastModification = lastModificationTime,
                SizeInBytes = count,
                UserId = userId,
                GroupId = groupId,
                Mode = mode,
                EntryType = entryType
            };
            await WriteAsync(header.GetHeaderValue(), 0, header.HeaderSize);
        }

        protected string GetPath(string realPath)
        {
            if (_relativeToPath == null)
            {
                return realPath;
            }

            //relativeToPath = "C:\windows\temp"
            //realPath = C:\windows\temp\tar\tar2
            // expected outcome
            //tar\tar2
            var toLower = realPath.ToLowerInvariant();
            int extra = _relativeToPath.EndsWith(Path.DirectorySeparatorChar.ToString()) ? 0 : 1;
            if (toLower.IndexOf(_relativeToPath) > -1)
            {
                return realPath.Substring(_relativeToPath.Length + extra);
            }

            return realPath;
        }

        public async Task AlignTo512Async(long size, bool acceptZero)
        {
            size %= 512;
            
            if (size == 0 && !acceptZero) return;
            if (size == 0)
            {
                size = 512;
            }
            else
            {
                size = 512-size;
            }

            await OutStream.WriteAsync(_zeroes, 0, (int)size);
        }

        public virtual async Task Close()
        {
            if (_isClosed) return;
            await AlignTo512Async(0, true);
            await AlignTo512Async(0, true);
            _isClosed = true;
        }

        protected async Task WriteAsync(byte[] buffer, int offset, int length)
        {
            await OutStream.WriteAsync(buffer, offset, length);
        }

        protected async Task CopyStreamAsync(Stream sourceStream)
        {
            int bytesRead = await sourceStream.ReadAsync(_buffer, 0, _buffer.Length);
            while (bytesRead > 0)
            {
                await WriteAsync(_buffer, 0, bytesRead);

                bytesRead = await sourceStream.ReadAsync(_buffer, 0, _buffer.Length);
            }
        }
    }
}