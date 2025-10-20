using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography.X509Certificates;

namespace CSharpEssentials.HttpHelper;
public static class httpBindingCertificate {
    // DA INSERIRE in una CLASSE STATICA (es. httpExtension)

    public static IHttpClientBuilder getCertificateForHttpHandler(
        this IHttpClientBuilder builder, // <-- Questo lo rende un Extension Method per IHttpClientBuilder
        string certificatePath,
        string password) {
        // Aggiungi la configurazione del certificato come delegato
        builder.ConfigurePrimaryHttpMessageHandler(() => {
            // Creiamo un nuovo SocketsHttpHandler o HttpClientHandler per impostare il certificato
            var handler = new HttpClientHandler();

            if (!string.IsNullOrEmpty(certificatePath)) {
                try {
                    // La tua logica originale per caricare il certificato
                    var clientCertificate = new X509Certificate2(
                        certificatePath,
                        password,
                        X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet
                    );
                    handler.ClientCertificates.Add(clientCertificate);
                } catch (Exception ex) {
                    // La tua logica di errore originale
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Errore nel caricamento del certificato: {ex}");
                    Console.ResetColor();
                    // Potresti voler lanciare un'eccezione qui se il certificato è obbligatorio
                }
            }

            return handler;
        });

        // Ritorna l'IHttpClientBuilder per permettere il chaining
        return builder;
    }
    //public httpBindingCertificate() { }
    //HttpClientHandler instance { get; set; }
    //public HttpMessageHandler getCertificateForHttpHandler(string certificatePath, string password) {
    //    instance = new HttpClientHandler();
    //    if (!string.IsNullOrEmpty(certificatePath)) {
    //        try {
    //            var clientCertificate = new X509Certificate2(certificatePath, password, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
    //            instance.ClientCertificates.Add(clientCertificate);
    //        } catch (Exception ex) {
    //            Console.ForegroundColor = ConsoleColor.Red;
    //            Console.WriteLine(ex.ToString());
    //            Console.ResetColor();
    //        }
    //    }
    //    return instance;
    //}
}