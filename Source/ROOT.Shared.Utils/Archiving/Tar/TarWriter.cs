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
using System.Threading.Tasks;

namespace ROOT.Shared.Utils.Archiving.Tar
{
    public class TarWriter : LegacyTarWriter
    {
        public TarWriter(string fileName, string relativeToPath = null) : base(File.Create(fileName, 8192, FileOptions.Asynchronous), true, relativeToPath)
        {
        }

        protected override async Task WriteHeaderAsync(string name, DateTime lastModificationTime, long count, int userId, int groupId, int mode, EntryType entryType)
        {
            var modifiedPath = GetPath(name);
            Debug.WriteLine(modifiedPath);
            var tarHeader = new UsTarHeader
            {
                FileName = modifiedPath,
                LastModification = lastModificationTime,
                SizeInBytes = count,
                UserId = userId,
                UserName = Convert.ToString(userId, 8),
                GroupId = groupId,
                GroupName = Convert.ToString(groupId, 8),
                Mode = mode,
                EntryType = entryType
            };
            await WriteAsync(tarHeader.GetHeaderValue(), 0, tarHeader.HeaderSize);
        }

        protected virtual async Task WriteHeaderAsync(string name, DateTime lastModificationTime, long count, string userName, string groupName, int mode)
        {
            var modifiedPath = GetPath(name);
            Debug.WriteLine(modifiedPath);
            var tarHeader = new UsTarHeader
            {
                FileName = modifiedPath,
                LastModification = lastModificationTime,
                SizeInBytes = count,
                UserId = userName.GetHashCode(),
                UserName = userName,
                GroupId = groupName.GetHashCode(),
                GroupName = groupName,
                Mode = mode
            };
            await WriteAsync(tarHeader.GetHeaderValue(), 0, tarHeader.HeaderSize);
        }

        public async Task WriteAsync(Stream data, long dataSizeInBytes, string fileName, string userId, string groupId, int mode,
                          DateTime lastModificationTime)
        {
            await WriteHeaderAsync(fileName, lastModificationTime, dataSizeInBytes, userId, groupId, mode);
            await CopyStreamAsync(data);
            await AlignTo512Async(dataSizeInBytes, false);
        }
    }
}