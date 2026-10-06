using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using PaletsWebApp.Utilites;
using System.IO;
using System.Threading.Tasks;

namespace PaletsWebApp;
public class FirebaseStorageService
{
    private readonly string _bucketName = "portal-bonilla.appspot.com"; // Reemplaza con tu nombre de bucket
    private readonly StorageClient _storageClient;

    public FirebaseStorageService()
    {
        _storageClient = StorageClient.Create(Utils.GetFirebaseCredential());
    }

    public async Task<string> UploadImageAsync(Stream imageStream, string fileName)
    {
        // Asegúrate de que el nombre del archivo contenga la extensión
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension))
        {
            throw new ArgumentException("El archivo debe tener una extensión para determinar el tipo de imagen.");
        }

        // Crea el nombre del objeto con la extensión incluida
        var imageObjectName = $"palets/{fileName}";

        // Sube la imagen al bucket con el nombre completo
        await _storageClient.UploadObjectAsync(_bucketName, imageObjectName, "image/" + extension.Trim('.'), imageStream);

        // Generar la URL pública de Firebase Storage con el token
        string publicUrl = $"https://firebasestorage.googleapis.com/v0/b/{_bucketName}/o/{Uri.EscapeDataString(imageObjectName)}?alt=media&token={Guid.NewGuid()}";



        return publicUrl;
    }

}
