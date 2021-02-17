using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NaCl;

namespace Lizelaser0310.Utilities
{
    public static class AuthUtility
    {
        public static string HashPassword(string pass, byte[] encryptionKey)
        {
            byte[] nonce = CreateNonce();

            var password = Encoding.UTF8.GetBytes(pass);
            var cipher = new byte[password.Length + XSalsa20Poly1305.TagLength];

            XSalsa20Poly1305 xSalsa20Poly1305 = new XSalsa20Poly1305(encryptionKey);
            xSalsa20Poly1305.Encrypt(cipher, password, nonce);

            string p = string.Concat(Convert.ToBase64String(nonce), "|", Convert.ToBase64String(cipher));

            return p;
        }


        public static bool VerifyPassword(string password, string rawPassword, byte[] encryptionKey)
        {
            XSalsa20Poly1305 xSalsa20Poly1305 = new XSalsa20Poly1305(encryptionKey);

            string[] credentials = rawPassword.Split('|');
            byte[] nonce0 = Convert.FromBase64String(credentials[0]);
            byte[] cipher0 = Convert.FromBase64String(credentials[1]);

            var pass = Encoding.UTF8.GetBytes(password);

            var cipher = new byte[pass.Length + XSalsa20Poly1305.TagLength];

            xSalsa20Poly1305.Encrypt(cipher, pass, nonce0);

            return cipher0.SequenceEqual(cipher);
        }
        private static byte[] CreateNonce()
        {
            using var rng = RandomNumberGenerator.Create();
            var nonce = new byte[XSalsa20Poly1305.NonceLength];
            rng.GetBytes(nonce);

            return nonce;
        }
    }
}