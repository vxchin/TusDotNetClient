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

        public TusClientTests()
        {
            _dataDirectoryPath = Fixture.DataDirectory.FullName;
        }
        
        [Theory]
        [MemberData(nameof(Fixture.GetTestFiles), MemberType = typeof(Fixture))]
        public void AfterCallingCreate_DataShouldContainAFile(FileInfo file)
        {
            var sut = new TusClient();


            var url = sut.Create(
                "http://localhost:1080/files/",
                file);


            var upload = new FileInfo(Path.Combine(_dataDirectoryPath, $"{url.Split('/').Last()}.bin"));
            upload.Exists.ShouldBe(true);
            upload.Length.ShouldBe(0);
        }

        [Theory]
        [MemberData(nameof(Fixture.GetTestFiles), MemberType = typeof(Fixture))]
        public void AfterCallingCreateAndUpload_UploadedFileShouldBeTheSameAsTheOriginalFile(FileInfo file)
        {
            var sut = new TusClient();


            var url = sut.Create("http://localhost:1080/files/", file);
            sut.Upload(url, file);


            var upload = new FileInfo(Path.Combine(_dataDirectoryPath, $"{url.Split('/').Last()}.bin"));
            upload.Exists.ShouldBe(true);
            upload.Length.ShouldBe(file.Length);
            using (var fileStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
            using (var uploadStream = new FileStream(upload.FullName, FileMode.Open, FileAccess.Read))
            {
                var fileBytes = new byte[fileStream.Length];
                fileStream.Read(fileBytes, 0, fileBytes.Length);
                var uploadBytes = new byte[uploadStream.Length];
                uploadStream.Read(uploadBytes, 0, uploadBytes.Length);
                SHA1(uploadBytes).ShouldBe(SHA1(fileBytes));
            }
        }

        [Theory]
        [MemberData(nameof(Fixture.GetTestFiles), MemberType = typeof(Fixture))]
        public void AfterCallingDownload_DownloadedFileShouldBeTheSameAsTheOriginalFile(FileInfo file)
        {
            var sut = new TusClient();


            var url = sut.Create("http://localhost:1080/files/", file);
            sut.Upload(url, file);
            var response = sut.Download(url);


            using (var fileStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
            {
                var fileBytes = new byte[fileStream.Length];
                fileStream.Read(fileBytes, 0, fileBytes.Length);
                SHA1(response.ResponseBytes).ShouldBe(SHA1(fileBytes));
            }
        }

        [Theory]
        [MemberData(nameof(Fixture.GetTestFiles), MemberType = typeof(Fixture))]
        public void CallingHead_ShouldReturnProgressOfUploadedFile(FileInfo file)
        {
            var sut = new TusClient();


            var url = sut.Create("http://localhost:1080/files/", file);
            var headBeforeUpload = sut.Head(url);
            sut.Upload(url, file);
            var headAfterUpload = sut.Head(url);


            headBeforeUpload.Headers.Keys.ShouldContain("Upload-Offset");
            headBeforeUpload.Headers["Upload-Offset"].ShouldBe("0");
            headAfterUpload.Headers.Keys.ShouldContain("Upload-Offset");
            headAfterUpload.Headers["Upload-Offset"].ShouldBe(file.Length.ToString());
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

        [Theory]
        [MemberData(nameof(Fixture.GetTestFiles), MemberType = typeof(Fixture))]
        public void CallingDelete_ShouldRemoveUploadedFile(FileInfo file)
        {
            var sut = new TusClient();


            var url = sut.Create("http://localhost:1080/files/", file);
            sut.Upload(url, file);
            var uploadHeadResponse = sut.Head(url);
            var deleteResult = sut.Delete(url);
            
            
            deleteResult.ShouldBe(true);
            uploadHeadResponse.Headers.Keys.ShouldContain("Upload-Offset");
            uploadHeadResponse.Headers["Upload-Offset"].ShouldBe(file.Length.ToString());
            File.Exists(Path.Combine(_dataDirectoryPath, $"url.Split('/').Last().bin")).ShouldBe(false);
        }
    }
}