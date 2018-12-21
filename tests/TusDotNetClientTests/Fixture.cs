using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace TusDotNetClientTests
{
    public class Fixture : IDisposable
    {
        private readonly Process _tusProcess;

        public DirectoryInfo DataDirectory;
        public FileInfo SmallTextFile;
        
        public Fixture()
        {
            DataDirectory = Directory.CreateDirectory("data");
            _tusProcess = Process.Start(new DirectoryInfo(Directory.GetCurrentDirectory())
                                            .Parent?
                                            .Parent?
                                            .Parent?
                                            .EnumerateFiles("tusd*")
                                            .First()
                                            .FullName ??
                                        throw new ArgumentException(
                                            "tusd executable must be present in test project directory"));
            
            SmallTextFile = new FileInfo(Path.Combine(DataDirectory.FullName, "small_text_file.txt"));
            File.WriteAllText(SmallTextFile.FullName, Guid.NewGuid().ToString());
        }
        
        public void Dispose()
        {
            _tusProcess.Kill();
            DataDirectory.Delete(true);
        }
    }
}