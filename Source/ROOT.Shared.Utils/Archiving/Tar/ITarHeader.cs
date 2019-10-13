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

namespace ROOT.Shared.Utils.Archiving.Tar
{
    public enum EntryType : byte
    {
        File = 0,
        FileObsolete = 0x30,
        HardLink = 0x31,
        SymLink = 0x32,
        CharDevice = 0x33,
        BlockDevice = 0x34,
        Directory = 0x35,
        Fifo = 0x36,
    }

    public interface ITarHeader
    {
        string FileName { get; set; }
        int Mode { get; set; }
        int UserId { get; set; }
        string UserName { get; set; }
        int GroupId { get; set; }
        string GroupName { get; set; }
        long SizeInBytes { get; set; }
        DateTime LastModification { get; set; }
        int HeaderSize { get; }
        EntryType EntryType { get; set; }
    }
}