using Org.BouncyCastle.OpenSsl;

namespace Netigent.Utils.FileStoreIO.Models
{
    public class PasswordFinder : IPasswordFinder
    {
        private string password;
        public PasswordFinder(string _password) { password = _password; }
        public char[] GetPassword() { return password.ToCharArray(); }
    }
}
