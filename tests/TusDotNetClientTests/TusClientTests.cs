using System;
using System.IO;
using System.Linq;
using Shouldly;
using TusDotNetClient;
using Xunit;
using static TusDotNetClientTests.Utils;

namespace TusDotNetClientTests
{
    public class TusClientTests : IClassFixture<Fixture>
    {
        private readonly string _dataDirectoryPath;
        private readonly FileInfo _smallTextFile;

        public TusClientTests(Fixture fixture)
        {
            _dataDirectoryPath = fixture.DataDirectory.FullName;
            _smallTextFile = fixture.SmallTextFile;
        }
        
        [Fact]
        public void AfterCallingCreate_DataShouldContainAFile()
        {
            var sut = new TusClient();


            var url = sut.Create(
                "http://localhost:1080/files/",
                _smallTextFile);


            var upload = new FileInfo(Path.Combine(_dataDirectoryPath, $"{url.Split('/').Last()}.bin"));
            upload.Exists.ShouldBe(true);
            upload.Length.ShouldBe(0);
        }

        [Fact]
        public void AfterCallingCreateAndUpload_UploadedFileShouldBeTheSameAsTheOriginalFile()
        {
            var sut = new TusClient();


            var url = sut.Create("http://localhost:1080/files/", _smallTextFile, ("Content-Type", "text/plain"));
            sut.Upload(url, _smallTextFile);


            var upload = new FileInfo(Path.Combine(_dataDirectoryPath, $"{url.Split('/').Last()}.bin"));
            upload.Exists.ShouldBe(true);
            upload.Length.ShouldBe(_smallTextFile.Length);
            using (var fileStream = new FileStream(_smallTextFile.FullName, FileMode.Open, FileAccess.Read))
            using (var uploadStream = new FileStream(upload.FullName, FileMode.Open, FileAccess.Read))
            {
                var fileBytes = new byte[fileStream.Length];
                fileStream.Read(fileBytes, 0, fileBytes.Length);
                var uploadBytes = new byte[uploadStream.Length];
                uploadStream.Read(uploadBytes, 0, uploadBytes.Length);
                SHA1(uploadBytes).ShouldBe(SHA1(fileBytes));
            }
        }

        [Fact]
        public void AfterCallingDownload_DownloadedFileShouldBeTheSameAsTheOriginalFile()
        {
            var sut = new TusClient();


            var url = sut.Create("http://localhost:1080/files/", _smallTextFile, ("Content-Type", "text/plain"));
            sut.Upload(url, _smallTextFile);
            var response = sut.Download(url);


            using (var fileStream = new FileStream(_smallTextFile.FullName, FileMode.Open, FileAccess.Read))
            {
                var fileBytes = new byte[fileStream.Length];
                fileStream.Read(fileBytes, 0, fileBytes.Length);
                SHA1(response.ResponseBytes).ShouldBe(SHA1(fileBytes));
            }
        }

        [Fact]
        public void CallingHead_ShouldReturnProgressOfUploadedFile()
        {
            var sut = new TusClient();


            var url = sut.Create("http://localhost:1080/files/", _smallTextFile, ("Content-Type", "text/plain"));
            var headBeforeUpload = sut.Head(url);
            sut.Upload(url, _smallTextFile);
            var headAfterUpload = sut.Head(url);


            headBeforeUpload.Headers.Keys.ShouldContain("Upload-Offset");
            headBeforeUpload.Headers["Upload-Offset"].ShouldBe("0");
            headAfterUpload.Headers.Keys.ShouldContain("Upload-Offset");
            headAfterUpload.Headers["Upload-Offset"].ShouldBe(_smallTextFile.Length.ToString());
        }

        [Fact]
        public void CallingGetServerInfo_ShouldReturnServerInfo()
        {
            var sut = new TusClient();


            var response = sut.GetServerInfo("http://localhost:1080/files/");
            
            
            response.Version.ShouldNotBeNullOrWhiteSpace();
            response.Extensions.ShouldNotBeNullOrWhiteSpace();
            response.SupportedVersions.ShouldNotBeNullOrWhiteSpace();
        }

        [Fact]
        public void CallingDelete_ShouldRemoveUploadedFile()
        {
            var sut = new TusClient();


            var url = sut.Create("http://localhost:1080/files/", _smallTextFile, ("Content-Type", "text/plain"));
            sut.Upload(url, _smallTextFile);
            var uploadHeadResponse = sut.Head(url);
            var deleteResult = sut.Delete(url);
            
            
            deleteResult.ShouldBe(true);
            uploadHeadResponse.Headers.Keys.ShouldContain("Upload-Offset");
            uploadHeadResponse.Headers["Upload-Offset"].ShouldBe(_smallTextFile.Length.ToString());
            File.Exists(Path.Combine(_dataDirectoryPath, $"url.Split('/').Last().bin")).ShouldBe(false);
        }
    }
}