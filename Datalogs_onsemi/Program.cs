using System;
using System.IO;

namespace FileMover
{
    class Program
    {
        static void Main(string[] args)
        {
            // Define source and destination directories
            string sourceDir = @"C:\Users\Ezer\Desktop\datalogs\Ondatalog\ASL1K_DATALOG";
            string backupDir = @"C:\Users\Ezer\Desktop\datalogs\Ondatalog\backup";

            try
            {
                // Ensure the source directory exists
                if (Directory.Exists(sourceDir))
                {
                    // Get all subdirectories in the source directory
                    var subdirectories = Directory.GetDirectories(sourceDir);

                    // Iterate over each subdirectory
                    foreach (var subdirectory in subdirectories)
                    {
                        // Get all files in the subdirectory
                        var files = Directory.GetFiles(subdirectory);

                        // Iterate over each file
                        foreach (var file in files)
                        {
                            // Extract the file name
                            var fileName = Path.GetFileName(file);

                            // Extract the lot number (assuming the lot number is the first part of the file name)
                            var lotNumber = fileName.Split('_')[0];

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
                    }

                    Console.WriteLine("All files have been moved successfully.");
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
