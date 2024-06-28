using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Renci.SshNet;

namespace FileMover
{
    class Program
    {
        static void Main(string[] args)
        {
            // Define source and destination directories
            string sourceDir = @"C:\Users\Ezer\Desktop\datalogs\Ondatalog\ASL1K_DATALOG";
            string backupDir = @"C:\Users\Ezer\Desktop\datalogs\Ondatalog\backup";
            string backupDir2 = @"C:\Users\Ezer\Desktop\datalogs\Ondatalog\backup 2";
            string zipDir = @"C:\Users\Ezer\Desktop\datalogs\Ondatalog\ASL ZIP";
            string sftpHost = "sftp4.atecphil.com";
            string sftpUser = "onsemi_system";
            string sftpPassword = "ONSemi1*";
            string sftpRemoteDir = "/files/ASL/";

            // HashSet to store names of transferred ZIP files
            var transferredFiles = new HashSet<string>();

            try
            {
                // Ensure the source directory exists
                if (Directory.Exists(sourceDir))
                {
                    // Get all files recursively in the source directory
                    var files = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);

                    // Process each file
                    foreach (var file in files)
                    {
                        try
                        {
                            // Extract the file name
                            var fileName = Path.GetFileName(file);

                            // Extract the lot number and identifier (e.g., P1, P2, R1, etc.)
                            var parts = fileName.Split('_');
                            if (parts.Length < 3) continue;

                            var lotNumber = parts[0];
                            var identifier = parts[2].Split('.')[0];

                            // Check if the identifier is one of the required patterns
                            if (identifier != "P1" && identifier != "P2" && identifier != "R1" && identifier != "R2" && identifier != "QA" && identifier != "Q1" && identifier != "Q2")
                            {
                                continue;
                            }

                            if (!string.IsNullOrEmpty(lotNumber))
                            {
                                // Define the new directory path in the backup folder
                                string newDir = Path.Combine(backupDir, lotNumber);

                                // Create the new directory if it doesn't exist
                                if (!Directory.Exists(newDir))
                                {
                                    Directory.CreateDirectory(newDir);
                                }

                                // Define the new file path
                                string newFilePath = Path.Combine(newDir, fileName);

                                // Move the file to the new directory
                                File.Move(file, newFilePath);

                                Console.WriteLine($"Moved {fileName} to {newFilePath}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"An error occurred while processing the file {file}: {ex.Message}");
                        }
                    }

                    // Process backup directory for ZIP creation
                    var directories = Directory.GetDirectories(backupDir);
                    foreach (var dir in directories)
                    {
                        var filesInDir = Directory.GetFiles(dir);
                        var groupedFiles = filesInDir
                            .Where(f => f.Contains("P1") || f.Contains("P2") || f.Contains("R1") || f.Contains("R2") || f.Contains("QA") || f.Contains("Q1") || f.Contains("Q2"))
                            .GroupBy(f => Path.GetFileNameWithoutExtension(f).Split('_')[0] + "_" + Path.GetFileNameWithoutExtension(f).Split('_')[1] + "_" + Path.GetFileNameWithoutExtension(f).Split('_')[2]);

                        foreach (var group in groupedFiles)
                        {
                            string lotAndCode = group.Key;
                            var lsrFile = group.FirstOrDefault(f => f.EndsWith(".lsr"));
                            var spdFile = group.FirstOrDefault(f => f.EndsWith(".spd"));

                            if (lsrFile != null && spdFile != null)
                            {
                                string zipFileName = Path.Combine(zipDir, lotAndCode + ".zip");

                                // Check if the ZIP file already exists or has been transferred
                                if (transferredFiles.Contains(zipFileName))
                                {
                                    Console.WriteLine($"ZIP already transferred: {zipFileName}");
                                    continue;
                                }

                                // Create the ZIP file
                                using (FileStream zipToOpen = new FileStream(zipFileName, FileMode.Create))
                                {
                                    using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                                    {
                                        archive.CreateEntryFromFile(lsrFile, Path.GetFileName(lsrFile));
                                        archive.CreateEntryFromFile(spdFile, Path.GetFileName(spdFile));
                                    }
                                }

                                Console.WriteLine($"Created ZIP: {zipFileName}");

                                // Transfer the ZIP file to the SFTP server
                                try
                                {
                                    using (var sftp = new SftpClient(sftpHost, sftpUser, sftpPassword))
                                    {
                                        sftp.Connect();
                                        Console.WriteLine("Connected to SFTP server.");

                                        using (var fileStream = new FileStream(zipFileName, FileMode.Open))
                                        {
                                            sftp.UploadFile(fileStream, Path.Combine(sftpRemoteDir, Path.GetFileName(zipFileName)));
                                        }

                                        Console.WriteLine($"Transferred {zipFileName} to SFTP server.");
                                        sftp.Disconnect();
                                    }

                                    // Add the transferred ZIP file to the HashSet
                                    transferredFiles.Add(zipFileName);

                                    // Move files to backup 2 after successful ZIP creation and transfer
                                    foreach (var file in group)
                                    {
                                        string lotNumber = Path.GetFileNameWithoutExtension(file).Split('_')[0];
                                        string backupDir2Lot = Path.Combine(backupDir2, lotNumber);

                                        // Create the lot number directory in backup 2 if it doesn't exist
                                        if (!Directory.Exists(backupDir2Lot))
                                        {
                                            Directory.CreateDirectory(backupDir2Lot);
                                        }

                                        string backupFilePath = Path.Combine(backupDir2Lot, Path.GetFileName(file));

                                        // Move the file to backup 2
                                        File.Move(file, backupFilePath);

                                        Console.WriteLine($"Moved {file} to {backupFilePath}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"An error occurred while transferring the file {zipFileName} to SFTP server: {ex.Message}");
                                }
                            }
                        }
                    }

                    Console.WriteLine("All files have been moved, zipped, transferred, and backed up successfully.");
                }
                else
                {
                    Console.WriteLine("The source directory does not exist.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred: {e.Message}");
            }
        }
    }
}
