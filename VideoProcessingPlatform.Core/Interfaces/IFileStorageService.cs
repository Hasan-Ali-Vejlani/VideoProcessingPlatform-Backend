// VideoProcessingPlatform.Core/Interfaces/IFileStorageService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace VideoProcessingPlatform.Core.Interfaces
{
    // Interface for abstracting file storage operations, including chunking and merging.
    public interface IFileStorageService
    {
        // Stores a single chunk of a file. Returns the path/identifier of the stored chunk.
        Task<string> StoreChunk(Guid uploadId, int chunkIndex, Stream chunkData);

        // Merges all stored chunks for a given upload ID into a single final file.
        // Returns the storage path of the merged file.
        Task<string> MergeChunks(Guid uploadId, string originalFileName, int totalChunks);

        // Retrieves a file stream from the storage service given its path.
        Task<Stream> RetrieveFile(string path);

        // Stores a generated thumbnail image. Returns the storage path of the thumbnail.
        Task<string> StoreThumbnail(Guid videoId, byte[] imageData, int index);

        // Stores multiple video renditions. Returns a list of storage paths for the renditions.
        Task<List<string>> StoreRenditions(Guid jobId, Dictionary<string, Stream> renditions);
    }
}
