using Netigent.Utils.FileStoreIO;
using Netigent.Utils.FileStoreIO.Models;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        BoxConfig config = new BoxConfig()
        {
            EnterpriseID = "194280382",
            BoxAppSettings = new BoxAppSettings()
            {
                ClientID = "nzelp8ltkens7475ji9d75uukdz15fe3",
                ClientSecret = "ynV7mj9YUhz7pgxTrZfYbE2gXVif1akZ",
                AppAuth = new AppAuth()
                {
                    Passphrase = "1f13d074bf586a2c0da2b9448e330e21",
                    PrivateKey = "-----BEGIN ENCRYPTED PRIVATE KEY-----\nMIIFDjBABgkqhkiG9w0BBQ0wMzAbBgkqhkiG9w0BBQwwDgQIy/jTWjfV6rwCAggA\nMBQGCCqGSIb3DQMHBAgblF/XUn7llwSCBMj0NOExPGbB70R+P6+YsESBY40AV5OJ\nvXqHPBFpu0AlORZwAfuIQCi5hTbaNb8w8UXkmsyhQSxiZYFvyTe8fpCoh2UphEd0\nHoZIZ6oxButEoe9xJ7V5ZH+wa82W+twaB+z937PVFi16tpQgNezZQuefTzxc9GME\nlQjW20ZmGt4EBbRXMHbLcRqxz9sB7TEd2tiZlc1g5CxIj1JPAzjDUKhohJvJpWu8\n1QObS9BNaPZQNzYd5tHGk3JbpDtoAdQOnMVDXVLOJnsvN8dpLEvO6sHczwXttdYA\nJS3Em7cLmFkSdBYlJfTTmNutjJo/RoYXN+YK2cxZciT05SBNYkS33zTM2KLH65Jj\nC8maQQutH9jgk/ijMYFJllJmI2jTFoCj268g+wXsx32/4KcDZPfY2OFwUm7n8wEW\nP7cXmtybLim1wL2tHfPUlcrW1i7O2fzxb7k0qL4HQxftddScfrd5aHK7f3oyo+Ah\nd8r88T/jIfu7fZ77xbDkROsJDr7CKC8d7Q41rhJG6v3bn62wKFVdHl+487RQ8VLp\nwzQ/5k+E0iuTAObqFdjVdntb+tljBmpzh8KMOJa0/q9uXp+8f9N+cdCR66qeul9p\nBFtXyCMSYPs4TlhCatR01FSlllNtDnpW21iBvSmvhGjpEjG6ElYa6g9qUNPEiSCp\na0Ze0u7ZioElg2Jqug6KUhGzPq5YydinYPlVOojioCbWhJoVs2KEdbTVnosg5tIj\nkmUViAtevYA+qwVbJKx+rEUueAe2FmmPXzbxJKJAgp5QELNgvJiz3ZBJ0E1lJd0z\nWBftfidcL3wxOMo6FM3BdK1SUV94gGzmmBI6zt314m3a6DcVFAFEA52QGxN/oCcS\np+vrrxD7D90zMMCW5oo5CaWJs4y9EFWQQlCjHg8xAdzW4B1T7Q9i3rzhVeEEtzyR\nBgliugxCK32AJiYBNR3em2IirzMACkKrxMpKZbz3YKkDZDAK9IewfymRQ3Mf6H8Z\nlszXYHAjCDA0bqLRJlVCQVa8EO+nmQmJbw+7ymcu3anXuBKpdPyc3wJk4b/gpucA\nYObDcZqqdnHNTOXEY4kLU/yTrafo8v8D0Uj16AQvzkypkO/RCbabzboD5t/9pjYx\n7JaMUY9j3HgkZ8VJLLBfR7jw6RuGXBrSK9206gcUJfghfyeI5JvgFw0GAjm8+QeB\nvO0nKnnUnSJrEma62+vBe4Dgjf5lyHYJ/GsR+1yi2jig0GuKuexvjcoD5uRa7Tuy\nHBUizl/Tau+EPa7bcAQeEX8/JNbJyen7pmn6acUVEXReli/A26jnxlg1BqNNPc8k\nIIS2HQx7DLZwamh+j69bZovCf3dXTNp+ydpERkhzAXg9p9xR03D5ClkVR81eTS+e\n4H9NGdI5qjrGOXofXHUwDFFDVOltoRJxaNcvUdW8S3GBg1gENqKGGnJ8QNL8cygU\nGu5d+cJBPBBjVSi9lpopLOxRbrABkuwQyjcwgMqZt/4i5UmGGQGNrX3thQjZ1p7K\nsaI1ubgwNrOyRkySKhSRj7TtmpqWY5JHZTaQgBjGQ2xO8bGYVRK14F6wN4FSn9jD\nTDTyilgZHJ2Egkrn1KKm8mTAVoW0TQ2G6zAAKUJ7O2u8EzV7NMRWS2BjbBFSMoLQ\nW5s=\n-----END ENCRYPTED PRIVATE KEY-----\n",
                    PublicKeyID = "oflk0r4k",
                },
            },
        };

        IFileStoreIOClient client = new FileStoreIOClient(
            databaseConnection: "Server=.;Database=Hcai;UID=SA;PWD=abc1234==DEV",
            fileStoreRoot: "0",
            maxVersions: 2,
            dbSchema: "filestore",
            defaultFileStore: Netigent.Utils.FileStoreIO.Enum.FileStorageProvider.Box,
            boxConfig: config);


        FileInfo fi = new FileInfo("C:\\temp\\Ingress\\logs\\log_20230104.log");
        byte[] contents = File.ReadAllBytes(fi.FullName);

        client.File_Upsert(
            fileContents: contents,
            fullFilename: fi.Name,
            fileLocation: Netigent.Utils.FileStoreIO.Enum.FileStorageProvider.Box,
            description: fi.Name,
            mainGroup: "0",
            subGroup: "");



    }
}