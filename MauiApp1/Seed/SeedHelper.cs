using System.IO;
using System.Text;
using System.Threading.Tasks;
#if ANDROID
using Android.Content.Res;
#endif
#if IOS
using Foundation;
#endif
using Microsoft.Maui.Storage;

public static class SeedHelper
{

    public static async Task<string> LoadMauiAsset(string fileName)
    {
        using var stream = FileSystem.OpenAppPackageFileAsync(fileName).Result;
        using var reader = new StreamReader(stream);

        var contents = reader.ReadToEnd();
        return contents;
    }
}
