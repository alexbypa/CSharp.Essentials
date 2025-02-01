using System.Security.Cryptography;
using System.Text;

namespace CSharpEssentials.EncryptHelper;
public static class MD5Helper {
    public static string toMD5(this string source) {
        using (var md5Hash = MD5.Create()) {
            var sourceBytes = Encoding.UTF8.GetBytes(source);
            var hashBytes = md5Hash.ComputeHash(sourceBytes);
            var hash = BitConverter.ToString(hashBytes).Replace("-", string.Empty);
            return hash;
        }
    }
}