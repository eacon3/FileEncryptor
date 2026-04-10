using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Konscious.Security.Cryptography;

namespace FileEncryptor
{
    // Encryption service class for handling file encryption and decryption operations
    public class EncryptionService
    {
        // Constants for encryption
        private const int KeySize = 128; // 128 bits for AES-GCM (16 bytes)
        private const int SaltSize = 16; // 128 bits salt for security
        private const int IvSize = 12; // 96 bits IV for AES-GCM
        private const int GcmTagLength = 16; // 128 bits authentication tag
        private const int DefaultIterations = 1; // Default Argon2id iterations
        private const int BufferSize = 8192; // 8KB buffer for files
        private const int AdditionalEntropySize = 16; // Additional entropy for unique encryption

        // Encryption strength levels
        public enum EncryptionStrength
        {
            Fast = 1,       // 1 iteration
            Medium = 10,    // 10 iterations
            High = 100,     // 100 iterations
            UltraHigh = 150 // 150 iterations
        }

        // Current encryption strength
        public EncryptionStrength CurrentStrength { get; set; } = EncryptionStrength.Fast;

        // Constants for file size thresholds
        private const long SmallFileThreshold = 100 * 1024 * 1024; // 100MB

        // Encrypts a single file with the specified password
        public async Task EncryptFileAsync(string inputFilePath, string outputFilePath, string password, Action<int> progressCallback = null)
        {
            if (!File.Exists(inputFilePath))
                throw new FileNotFoundException($"Input file does not exist: {inputFilePath}");

            // Check if file is too large (optional limit)
            FileInfo fileInfo = new FileInfo(inputFilePath);
            long fileSize = fileInfo.Length;
            
            // Check available disk space
            DriveInfo driveInfo = new DriveInfo(Path.GetPathRoot(outputFilePath));
            if (driveInfo.AvailableFreeSpace < fileSize * 2) // Ensure enough space for encryption
                throw new IOException($"Not enough disk space. Required: {fileSize * 2}, Available: {driveInfo.AvailableFreeSpace}");

            string tempOutputFilePath = $"{outputFilePath}.tmp";

            try
            {
                // Generate salt, IV, and additional entropy
                byte[] salt = new byte[SaltSize];
                byte[] iv = new byte[IvSize];
                byte[] additionalEntropy = new byte[AdditionalEntropySize];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(salt);
                    rng.GetBytes(iv);
                    rng.GetBytes(additionalEntropy);
                }

                // Update progress
                progressCallback?.Invoke(5);

                // Derive key from password with salt and additional entropy
                // Use lower iterations for small files to improve performance
                int originalStrength = (int)CurrentStrength;
                if (fileSize < 1024 * 1024) // Files smaller than 1MB
                {
                    CurrentStrength = EncryptionStrength.Fast; // 1 iteration for small files
                }

                byte[] key = await DeriveKeyAsync(password, salt, additionalEntropy);

                // Restore original strength
                CurrentStrength = (EncryptionStrength)originalStrength;

                // Update progress for key derivation
                progressCallback?.Invoke(20);

                // Calculate file checksum
                byte[] checksum;
                using (var inputStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan))
                {
                    checksum = await CalculateChecksumAsync(inputStream);
                }

                // Update progress for checksum calculation
                progressCallback?.Invoke(30);

                // Write header and encrypt file based on size
                if (fileSize <= SmallFileThreshold)
                {
                    // For small files: read entire file into memory
                    await EncryptSmallFileAsync(inputFilePath, tempOutputFilePath, key, iv, salt, additionalEntropy, checksum, progressCallback);
                }
                else
                {
                    // For large files: use stream-based processing
                    await EncryptLargeFileAsync(inputFilePath, tempOutputFilePath, key, iv, salt, additionalEntropy, checksum, fileSize, progressCallback);
                }

                // Rename temporary file to final output file
                if (File.Exists(outputFilePath))
                    File.Delete(outputFilePath);
                File.Move(tempOutputFilePath, outputFilePath);

                // Final progress update
                progressCallback?.Invoke(100);
            }
            catch
            {
                // Clean up temporary file if encryption fails
                if (File.Exists(tempOutputFilePath))
                    File.Delete(tempOutputFilePath);
                throw;
            }
        }

        // Encrypts small files by reading the entire file into memory
        private async Task EncryptSmallFileAsync(string inputFilePath, string outputFilePath, byte[] key, byte[] iv, byte[] salt, byte[] additionalEntropy, byte[] checksum, Action<int> progressCallback = null)
        {
            // Read entire file into memory
            byte[] fileContent = File.ReadAllBytes(inputFilePath);
            string originalExtension = Path.GetExtension(inputFilePath);

            using (var outputStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, FileOptions.Asynchronous))
            {
                // Write header to output file
                byte[] signature = Encoding.UTF8.GetBytes("ENCRYPT");
                await outputStream.WriteAsync(signature, 0, signature.Length);
                await outputStream.WriteAsync(salt, 0, salt.Length);
                await outputStream.WriteAsync(iv, 0, iv.Length);
                await outputStream.WriteAsync(additionalEntropy, 0, additionalEntropy.Length);
                await outputStream.WriteAsync(checksum, 0, checksum.Length);

                // Write original file extension
                byte[] extensionBytes = Encoding.UTF8.GetBytes(originalExtension);
                byte[] extensionLength = BitConverter.GetBytes(extensionBytes.Length);
                await outputStream.WriteAsync(extensionLength, 0, extensionLength.Length);
                await outputStream.WriteAsync(extensionBytes, 0, extensionBytes.Length);

                // Write compression flag (0 = no compression)
                await outputStream.WriteAsync(new byte[] { 0 }, 0, 1);

                // Encrypt entire file
                using (var aes = new AesGcm(key))
                {
                    byte[] ciphertext = new byte[fileContent.Length];
                    byte[] tag = new byte[GcmTagLength];
                    aes.Encrypt(iv, fileContent, ciphertext, tag);

                    // Write encrypted content and tag
                    await outputStream.WriteAsync(ciphertext, 0, ciphertext.Length);
                    await outputStream.WriteAsync(tag, 0, tag.Length);
                }

                await outputStream.FlushAsync();
            }

            // Update progress
            progressCallback?.Invoke(95);
        }

        // Encrypts large files using stream-based processing
        private async Task EncryptLargeFileAsync(string inputFilePath, string outputFilePath, byte[] key, byte[] iv, byte[] salt, byte[] additionalEntropy, byte[] checksum, long fileSize, Action<int> progressCallback = null)
        {
            string originalExtension = Path.GetExtension(inputFilePath);

            using (var inputStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan))
            using (var outputStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                // Write header to output file
                byte[] signature = Encoding.UTF8.GetBytes("ENCRYPT");
                await outputStream.WriteAsync(signature, 0, signature.Length);
                await outputStream.WriteAsync(salt, 0, salt.Length);
                await outputStream.WriteAsync(iv, 0, iv.Length);
                await outputStream.WriteAsync(additionalEntropy, 0, additionalEntropy.Length);
                await outputStream.WriteAsync(checksum, 0, checksum.Length);

                // Write original file extension
                byte[] extensionBytes = Encoding.UTF8.GetBytes(originalExtension);
                byte[] extensionLength = BitConverter.GetBytes(extensionBytes.Length);
                await outputStream.WriteAsync(extensionLength, 0, extensionLength.Length);
                await outputStream.WriteAsync(extensionBytes, 0, extensionBytes.Length);

                // Write compression flag (0 = no compression)
                await outputStream.WriteAsync(new byte[] { 0 }, 0, 1);

                // Stream-based encryption: encrypt in chunks without compression for better performance
                using (var aes = new AesGcm(key))
                {
                    int currentChunk = 0;
                    long totalBytesRead = 0;
                    int bytesRead;
                    byte[] buffer = new byte[BufferSize];

                    while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        // Generate unique nonce for each chunk
                        byte[] chunkNonce = new byte[IvSize];
                        Array.Copy(iv, 0, chunkNonce, 0, IvSize);
                        for (int i = 0; i < 4; i++)
                        {
                            chunkNonce[i] ^= (byte)((currentChunk >> (i * 8)) & 0xFF);
                        }

                        // Encrypt the chunk directly from buffer to save memory
                        byte[] ciphertext = new byte[bytesRead];
                        byte[] tag = new byte[GcmTagLength];
                        aes.Encrypt(chunkNonce, buffer.AsSpan(0, bytesRead), ciphertext, tag);

                        // Write encrypted chunk and tag
                        await outputStream.WriteAsync(ciphertext, 0, ciphertext.Length);
                        await outputStream.WriteAsync(tag, 0, tag.Length);

                        totalBytesRead += bytesRead;
                        currentChunk++;

                        // Update progress
                        int progress = 30 + (int)((double)totalBytesRead / fileSize * 65);
                        progressCallback?.Invoke(Math.Min(progress, 95));
                    }
                }

                await outputStream.FlushAsync();
            }
        }

        // Decrypts a single file with the specified password
        public async Task<string> DecryptFileAsync(string inputFilePath, string outputFilePath, string password, Action<int> progressCallback = null)
        {
            if (!File.Exists(inputFilePath))
                throw new FileNotFoundException($"Input file does not exist: {inputFilePath}");

            string tempOutputFilePath = $"{outputFilePath}.tmp";

            try
            {
                // Read header information first
                byte[] salt = new byte[SaltSize];
                byte[] iv = new byte[IvSize];
                byte[] additionalEntropy = new byte[AdditionalEntropySize];
                byte[] storedChecksum = new byte[32];
                string originalExtension;
                bool isCompressed;
                long totalEncryptedSize;

                using (var inputStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan))
                {
                    try
                    {
                        // Check minimum file size
                        if (inputStream.Length < 7 + SaltSize + IvSize + AdditionalEntropySize + 32 + 4 + 1)
                            throw new InvalidDataException("File is too small to be a valid encrypted file");

                        // Read header
                        byte[] signature = new byte[7];
                        int bytesRead = await inputStream.ReadAsync(signature, 0, signature.Length);
                        if (bytesRead != signature.Length)
                            throw new InvalidDataException("Failed to read file signature");
                            
                        string signatureString = Encoding.UTF8.GetString(signature);
                        if (signatureString != "ENCRYPT")
                            throw new InvalidDataException("Invalid encrypted file format");

                        // Read salt
                        bytesRead = await inputStream.ReadAsync(salt, 0, salt.Length);
                        if (bytesRead != salt.Length)
                            throw new InvalidDataException("Failed to read salt");

                        // Read IV
                        bytesRead = await inputStream.ReadAsync(iv, 0, iv.Length);
                        if (bytesRead != iv.Length)
                            throw new InvalidDataException("Failed to read IV");

                        // Read additional entropy
                        bytesRead = await inputStream.ReadAsync(additionalEntropy, 0, additionalEntropy.Length);
                        if (bytesRead != additionalEntropy.Length)
                            throw new InvalidDataException("Failed to read additional entropy");

                        // Read stored checksum
                        bytesRead = await inputStream.ReadAsync(storedChecksum, 0, storedChecksum.Length);
                        if (bytesRead != storedChecksum.Length)
                            throw new InvalidDataException("Failed to read stored checksum");

                        // Read extension length
                        byte[] extensionLengthBytes = new byte[4];
                        bytesRead = await inputStream.ReadAsync(extensionLengthBytes, 0, extensionLengthBytes.Length);
                        if (bytesRead != extensionLengthBytes.Length)
                            throw new InvalidDataException("Failed to read extension length");
                            
                        int extensionLength = BitConverter.ToInt32(extensionLengthBytes, 0);
                        
                        // Validate extension length
                        if (extensionLength < 0 || extensionLength > 100)
                            throw new InvalidDataException("Invalid extension length");

                        // Read extension
                        byte[] extensionBytes = new byte[extensionLength];
                        bytesRead = await inputStream.ReadAsync(extensionBytes, 0, extensionBytes.Length);
                        if (bytesRead != extensionBytes.Length)
                            throw new InvalidDataException("Failed to read extension");
                            
                        originalExtension = Encoding.UTF8.GetString(extensionBytes);

                        // Read compression flag
                        byte[] compressionFlagBytes = new byte[1];
                        bytesRead = await inputStream.ReadAsync(compressionFlagBytes, 0, compressionFlagBytes.Length);
                        if (bytesRead != compressionFlagBytes.Length)
                            throw new InvalidDataException("Failed to read compression flag");
                            
                        isCompressed = compressionFlagBytes[0] == 1;

                        // Calculate total encrypted size
                        totalEncryptedSize = inputStream.Length - (7 + SaltSize + IvSize + AdditionalEntropySize + 32 + 4 + extensionLength + 1);
                        
                        // Validate encrypted size
                        if (totalEncryptedSize <= 0)
                            throw new InvalidDataException("No encrypted data found");
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidDataException($"Error reading file header: {ex.Message}", ex);
                    }
                }

                try
                {
                    // Derive key from password with salt and additional entropy
                    byte[] key = await DeriveKeyAsync(password, salt, additionalEntropy);

                    // Update progress for key derivation
                    progressCallback?.Invoke(10);

                    // Check available disk space
                    DriveInfo driveInfo = new DriveInfo(Path.GetPathRoot(tempOutputFilePath));
                    // Estimate required space (assuming worst case: no compression)
                    long estimatedRequiredSpace = totalEncryptedSize * 2;
                    if (driveInfo.AvailableFreeSpace < estimatedRequiredSpace)
                        throw new IOException($"Not enough disk space. Required: {estimatedRequiredSpace}, Available: {driveInfo.AvailableFreeSpace}");

                    // Decrypt file based on size
                    if (totalEncryptedSize <= SmallFileThreshold)
                    {
                        // For small files: read entire file into memory
                        await DecryptSmallFileAsync(inputFilePath, tempOutputFilePath, key, iv, storedChecksum, isCompressed, progressCallback);
                    }
                    else
                    {
                        // For large files: use stream-based processing
                        await DecryptLargeFileAsync(inputFilePath, tempOutputFilePath, key, iv, storedChecksum, isCompressed, totalEncryptedSize, progressCallback);
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error during decryption process: {ex.Message}", ex);
                }

                try
                {
                    // Verify checksum
                    using (var checksumStream = new FileStream(tempOutputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize))
                    {
                        byte[] calculatedChecksum = await CalculateChecksumAsync(checksumStream);
                        if (!CompareByteArrays(storedChecksum, calculatedChecksum))
                            throw new InvalidDataException("File checksum mismatch, possibly corrupted file or wrong password");
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidDataException($"Error verifying checksum: {ex.Message}", ex);
                }

                // Update progress for checksum verification
                progressCallback?.Invoke(90);

                // Rename temporary file to final output file with original extension
                string finalOutputFilePath = outputFilePath + originalExtension;
                
                // Handle file name collision by appending number
                int counter = 1;
                string baseName = Path.GetFileNameWithoutExtension(finalOutputFilePath);
                string extension = Path.GetExtension(finalOutputFilePath);
                string directory = Path.GetDirectoryName(finalOutputFilePath);
                
                while (File.Exists(finalOutputFilePath))
                {
                    string newFileName = $"{baseName}{counter}{extension}";
                    finalOutputFilePath = Path.Combine(directory, newFileName);
                    counter++;
                }
                
                File.Move(tempOutputFilePath, finalOutputFilePath);

                // Final progress update
                progressCallback?.Invoke(100);
                
                return finalOutputFilePath;
            }
            catch (Exception ex)
            {
                // Clean up temporary file if decryption fails
                if (File.Exists(tempOutputFilePath))
                    File.Delete(tempOutputFilePath);
                // Log the error with stack trace for debugging
                Console.WriteLine($"Decryption error: {ex.ToString()}");
                throw;
            }
        }

        // Decrypts small files by reading the entire file into memory
        private async Task DecryptSmallFileAsync(string inputFilePath, string outputFilePath, byte[] key, byte[] iv, byte[] storedChecksum, bool isCompressed, Action<int> progressCallback = null)
        {
            try
            {
                // Use stream-based processing instead of reading entire file into memory
                using (var inputStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan))
                using (var outputStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, FileOptions.Asynchronous))
                {
                    // Reset file position to start
                    inputStream.Position = 0;
                    
                    // Read signature
                    byte[] signature = new byte[7];
                    int bytesRead = await inputStream.ReadAsync(signature, 0, signature.Length);
                    if (bytesRead != signature.Length)
                        throw new InvalidDataException("Failed to read file signature");
                    
                    string signatureString = Encoding.UTF8.GetString(signature);
                    if (signatureString != "ENCRYPT")
                        throw new InvalidDataException("Invalid encrypted file format");

                    // Read salt
                    byte[] salt = new byte[SaltSize];
                    bytesRead = await inputStream.ReadAsync(salt, 0, salt.Length);
                    if (bytesRead != salt.Length)
                        throw new InvalidDataException("Failed to read salt");

                    // Read IV
                    byte[] ivRead = new byte[IvSize];
                    bytesRead = await inputStream.ReadAsync(ivRead, 0, ivRead.Length);
                    if (bytesRead != ivRead.Length)
                        throw new InvalidDataException("Failed to read IV");

                    // Read additional entropy
                    byte[] additionalEntropy = new byte[AdditionalEntropySize];
                    bytesRead = await inputStream.ReadAsync(additionalEntropy, 0, additionalEntropy.Length);
                    if (bytesRead != additionalEntropy.Length)
                        throw new InvalidDataException("Failed to read additional entropy");

                    // Read stored checksum
                    byte[] storedChecksumRead = new byte[32];
                    bytesRead = await inputStream.ReadAsync(storedChecksumRead, 0, storedChecksumRead.Length);
                    if (bytesRead != storedChecksumRead.Length)
                        throw new InvalidDataException("Failed to read stored checksum");

                    // Read extension length
                    byte[] extensionLengthBytes = new byte[4];
                    bytesRead = await inputStream.ReadAsync(extensionLengthBytes, 0, extensionLengthBytes.Length);
                    if (bytesRead != extensionLengthBytes.Length)
                        throw new InvalidDataException("Failed to read extension length");
                    
                    int extensionLength = BitConverter.ToInt32(extensionLengthBytes, 0);
                    
                    // Validate extension length
                    if (extensionLength < 0 || extensionLength > 100)
                        throw new InvalidDataException("Invalid extension length");
                    
                    // Read extension
                    byte[] extensionBytes = new byte[extensionLength];
                    bytesRead = await inputStream.ReadAsync(extensionBytes, 0, extensionBytes.Length);
                    if (bytesRead != extensionBytes.Length)
                        throw new InvalidDataException("Failed to read extension");
                    
                    // Read compression flag
                    byte[] compressionFlagBytes = new byte[1];
                    bytesRead = await inputStream.ReadAsync(compressionFlagBytes, 0, compressionFlagBytes.Length);
                    if (bytesRead != compressionFlagBytes.Length)
                        throw new InvalidDataException("Failed to read compression flag");
                    
                    // Calculate remaining encrypted data
                    long remainingBytes = inputStream.Length - inputStream.Position;
                    if (remainingBytes <= 0)
                        throw new InvalidDataException("No encrypted data found");
                    
                    // Ensure we have enough data for at least one chunk and tag
                    if (remainingBytes < GcmTagLength)
                        throw new InvalidDataException("Incomplete data: not enough bytes for tag");

                    // Read encrypted content and tag
                    byte[] encryptedData = new byte[remainingBytes];
                    bytesRead = await inputStream.ReadAsync(encryptedData, 0, encryptedData.Length);
                    if (bytesRead != encryptedData.Length)
                        throw new InvalidDataException("Failed to read encrypted data");

                    // Decrypt content
                    using (var aes = new AesGcm(key))
                    {
                        // Calculate actual content length (excluding tag)
                        long contentLengthLong = remainingBytes - GcmTagLength;
                        if (contentLengthLong <= 0)
                            throw new InvalidDataException("No encrypted content found");
                        
                        // Ensure content length doesn't exceed int max value
                        if (contentLengthLong > int.MaxValue)
                            throw new InvalidDataException("File too large to decrypt as small file");
                        
                        int contentLength = (int)contentLengthLong;
                        
                        // Extract ciphertext and tag
                        byte[] ciphertext = new byte[contentLength];
                        byte[] tag = new byte[GcmTagLength];
                        Array.Copy(encryptedData, 0, ciphertext, 0, contentLength);
                        Array.Copy(encryptedData, contentLength, tag, 0, GcmTagLength);

                        // Decrypt
                        byte[] decryptedContent = new byte[contentLength];
                        aes.Decrypt(iv, ciphertext, tag, decryptedContent);

                        // Write decrypted content
                        await outputStream.WriteAsync(decryptedContent, 0, decryptedContent.Length);
                    }

                    await outputStream.FlushAsync();
                }

                // Update progress
                progressCallback?.Invoke(80);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error in DecryptSmallFileAsync: {ex.Message}", ex);
            }
        }

        // Decrypts large files using stream-based processing
        private async Task DecryptLargeFileAsync(string inputFilePath, string outputFilePath, byte[] key, byte[] iv, byte[] storedChecksum, bool isCompressed, long totalEncryptedSize, Action<int> progressCallback = null)
        {
            using (var inputStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                try
                {
                    // Reset file position to start
                    inputStream.Position = 0;
                    
                    // Read signature
                    byte[] signature = new byte[7];
                    int bytesRead = await inputStream.ReadAsync(signature, 0, signature.Length);
                    if (bytesRead != signature.Length)
                        throw new InvalidDataException("Failed to read file signature");
                    
                    string signatureString = Encoding.UTF8.GetString(signature);
                    if (signatureString != "ENCRYPT")
                        throw new InvalidDataException("Invalid encrypted file format");

                    // Read salt
                    byte[] salt = new byte[SaltSize];
                    bytesRead = await inputStream.ReadAsync(salt, 0, salt.Length);
                    if (bytesRead != salt.Length)
                        throw new InvalidDataException("Failed to read salt");

                    // Read IV
                    byte[] ivRead = new byte[IvSize];
                    bytesRead = await inputStream.ReadAsync(ivRead, 0, ivRead.Length);
                    if (bytesRead != ivRead.Length)
                        throw new InvalidDataException("Failed to read IV");

                    // Read additional entropy
                    byte[] additionalEntropy = new byte[AdditionalEntropySize];
                    bytesRead = await inputStream.ReadAsync(additionalEntropy, 0, additionalEntropy.Length);
                    if (bytesRead != additionalEntropy.Length)
                        throw new InvalidDataException("Failed to read additional entropy");

                    // Read stored checksum
                    byte[] storedChecksumRead = new byte[32];
                    bytesRead = await inputStream.ReadAsync(storedChecksumRead, 0, storedChecksumRead.Length);
                    if (bytesRead != storedChecksumRead.Length)
                        throw new InvalidDataException("Failed to read stored checksum");

                    // Read extension length
                    byte[] extensionLengthBytes = new byte[4];
                    bytesRead = await inputStream.ReadAsync(extensionLengthBytes, 0, extensionLengthBytes.Length);
                    if (bytesRead != extensionLengthBytes.Length)
                        throw new InvalidDataException("Failed to read extension length");
                    
                    int extensionLength = BitConverter.ToInt32(extensionLengthBytes, 0);
                    
                    // Validate extension length
                    if (extensionLength < 0 || extensionLength > 100)
                        throw new InvalidDataException("Invalid extension length");
                    
                    // Read extension
                    byte[] extensionBytes = new byte[extensionLength];
                    bytesRead = await inputStream.ReadAsync(extensionBytes, 0, extensionBytes.Length);
                    if (bytesRead != extensionBytes.Length)
                        throw new InvalidDataException("Failed to read extension");
                    
                    // Read compression flag
                    byte[] compressionFlagBytes = new byte[1];
                    bytesRead = await inputStream.ReadAsync(compressionFlagBytes, 0, compressionFlagBytes.Length);
                    if (bytesRead != compressionFlagBytes.Length)
                        throw new InvalidDataException("Failed to read compression flag");

                    using (var outputStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan))
                    {
                        // Stream-based decryption: decrypt in chunks
                        int currentChunk = 0;
                        long totalBytesProcessed = 0;
                        const int chunkSize = 8192; // Use smaller chunk size to avoid memory issues

                        using (var aes = new AesGcm(key))
                        {
                            // For non-compressed files, read directly from input stream
                            byte[] buffer = new byte[chunkSize + GcmTagLength];
                            while (totalBytesProcessed < totalEncryptedSize)
                            {
                                // Calculate remaining bytes to read
                                long remaining = totalEncryptedSize - totalBytesProcessed;
                                if (remaining <= 0)
                                    break;
                                
                                // Ensure we don't read more than chunk size
                                int bytesToRead = (int)Math.Min(remaining, chunkSize + GcmTagLength);
                                
                                // Read encrypted chunk and tag
                                int bytesReadFromStream = await inputStream.ReadAsync(buffer, 0, bytesToRead);
                                if (bytesReadFromStream == 0)
                                    break;
                                
                                // Ensure we have enough data for at least one chunk and tag
                                if (bytesReadFromStream < GcmTagLength)
                                    throw new InvalidDataException("Incomplete data: not enough bytes for tag");
                                
                                // Calculate actual chunk size (excluding tag)
                                int actualChunkSize = bytesReadFromStream - GcmTagLength;
                                if (actualChunkSize <= 0)
                                    break;
                                
                                // Extract tag
                                byte[] tag = new byte[GcmTagLength];
                                Array.Copy(buffer, actualChunkSize, tag, 0, GcmTagLength);
                                
                                // Generate unique nonce for each chunk
                                byte[] chunkNonce = new byte[IvSize];
                                Array.Copy(iv, 0, chunkNonce, 0, IvSize);
                                for (int i = 0; i < 4; i++)
                                {
                                    chunkNonce[i] ^= (byte)((currentChunk >> (i * 8)) & 0xFF);
                                }
                                
                                // Decrypt the chunk
                                byte[] decryptedChunk = new byte[actualChunkSize];
                                aes.Decrypt(chunkNonce, buffer.AsSpan(0, actualChunkSize), tag, decryptedChunk);
                                
                                // Write decrypted chunk to output stream
                                await outputStream.WriteAsync(decryptedChunk, 0, decryptedChunk.Length);
                                
                                totalBytesProcessed += bytesReadFromStream;
                                currentChunk++;
                                
                                // Update progress
                                int progress = 10 + (int)((double)totalBytesProcessed / totalEncryptedSize * 70);
                                progressCallback?.Invoke(Math.Min(progress, 80));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error in DecryptLargeFileAsync: {ex.Message}", ex);
                }
            }
        }

        // Encrypts multiple files with the same password
        public async Task BatchEncryptAsync(List<string> files, string outputDirectory, string password, Action<int> progressCallback = null)
        {
            if (files == null || files.Count == 0)
                throw new ArgumentException("No files to encrypt");

            if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            int totalFiles = files.Count;
            int processedFiles = 0;

            foreach (string file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                string outputFilePath = Path.Combine(outputDirectory, $"{fileName}.cls");

                await EncryptFileAsync(file, outputFilePath, password, (progress) =>
                {
                    int overallProgress = (int)(((processedFiles * 100) + progress) / totalFiles);
                    progressCallback?.Invoke(Math.Min(overallProgress, 95));
                });

                processedFiles++;
            }

            progressCallback?.Invoke(100);
        }

        // Decrypts multiple files with the same password
        public async Task BatchDecryptAsync(List<string> files, string outputDirectory, string password, Action<int> progressCallback = null)
        {
            if (files == null || files.Count == 0)
                throw new ArgumentException("No files to decrypt");

            if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            int totalFiles = files.Count;
            int processedFiles = 0;

            foreach (string file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                string outputFilePath = Path.Combine(outputDirectory, fileName);

                await DecryptFileAsync(file, outputFilePath, password, (progress) =>
                {
                    int overallProgress = (int)(((processedFiles * 100) + progress) / totalFiles);
                    progressCallback?.Invoke(Math.Min(overallProgress, 95));
                });

                processedFiles++;
            }

            progressCallback?.Invoke(100);
        }

        // Derives a cryptographic key from a password using Argon2id with additional entropy
        private async Task<byte[]> DeriveKeyAsync(string password, byte[] salt, byte[] additionalEntropy)
        {
            // Combine password and additional entropy for enhanced security
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] combinedInput = new byte[passwordBytes.Length + additionalEntropy.Length];
            Array.Copy(passwordBytes, 0, combinedInput, 0, passwordBytes.Length);
            Array.Copy(additionalEntropy, 0, combinedInput, passwordBytes.Length, additionalEntropy.Length);

            // Get iterations based on current encryption strength
            int iterations = (int)CurrentStrength;

            // Run key derivation on a separate thread to avoid blocking UI
            return await Task.Run(() =>
            {
                using (var argon2 = new Argon2id(combinedInput))
                {
                    argon2.Salt = salt;
                    argon2.DegreeOfParallelism = Math.Min(Environment.ProcessorCount, 4); // Use optimal number of threads
                    // Adjust memory size based on encryption strength
                    int memorySize;
                    if (iterations <= 10)
                    {
                        memorySize = 1024 * 16; // 16MB for fast mode
                    }
                    else if (iterations <= 100)
                    {
                        memorySize = 1024 * 32; // 32MB for medium mode
                    }
                    else
                    {
                        memorySize = 1024 * 64; // 64MB for high mode
                    }
                    argon2.MemorySize = memorySize;
                    argon2.Iterations = iterations;
                    return argon2.GetBytes(KeySize / 8);
                }
            });
        }

        // Calculates SHA-256 checksum for a file
        private async Task<byte[]> CalculateChecksumAsync(Stream stream)
        {
            using (var sha256 = SHA256.Create())
            {
                stream.Position = 0;
                byte[] checksum = await sha256.ComputeHashAsync(stream);
                stream.Position = 0;
                return checksum;
            }
        }

        // Compares two byte arrays for equality
        private bool CompareByteArrays(byte[] array1, byte[] array2)
        {
            if (array1.Length != array2.Length)
                return false;

            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                    return false;
            }

            return true;
        }

        // Generates a random password with specified requirements
        public static string GeneratePassword(int length, bool includeUppercase = true, bool includeLowercase = true, bool includeNumbers = true, bool includeSpecial = true)
        {
            const string uppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowercaseChars = "abcdefghijklmnopqrstuvwxyz";
            const string numberChars = "0123456789";
            const string specialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";

            string charSet = "";
            if (includeUppercase) charSet += uppercaseChars;
            if (includeLowercase) charSet += lowercaseChars;
            if (includeNumbers) charSet += numberChars;
            if (includeSpecial) charSet += specialChars;

            if (string.IsNullOrEmpty(charSet))
                throw new ArgumentException("At least one character set must be included");

            byte[] randomBytes = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            StringBuilder password = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                password.Append(charSet[randomBytes[i] % charSet.Length]);
            }

            return password.ToString();
        }
    }
}
