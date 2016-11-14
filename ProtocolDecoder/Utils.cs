﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace ProtocolDecoder
{
    class Utils
    {
        public static string BaseFileName = null;
        public static string ExtractedFileName = null;
        public static string ExtractedTextName = null;
        public static byte[] ConvertInputToByteArray(string input)
        {

            string hexString = Regex.Replace(input, @"\s|(0x)", "");
            if (hexString.Length == 0 || Regex.IsMatch(hexString, @"[^0-9a-fA-F]"))
            {
                return null;
            }

            byte[] bytes = new byte[hexString.Length / 2];

            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hexString.Substring(2 * i, 2), 16);
            }
            return bytes;
        }

        public static void InitBaseFileName(string fileName)
        {
            try
            {
                BaseFileName = Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName));
                ExtractedFileName = BaseFileName + "_raw.isf";
                ExtractedTextName = BaseFileName + "_raw.txt";
            }
            catch (Exception e)
            {
                BaseFileName = @"c:\ProtocolDecoder\log";
            }
        }
        
        public static bool IsValidIsf(string file)
        {
            if (!File.Exists(file) || (Path.GetExtension(file) != ".isf" && Path.GetExtension(file) != ".qmdl"))
            {
                return false;
            }
            return true;
        }

        public static bool IsValidBinary(string file)
        {
            FileInfo fileInfo = new FileInfo(file);
            if (!File.Exists(file) || fileInfo.Length >= 1000000)
            {
                return false;
            }
            return true;
        }
    }
}
