﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.ComponentModel;
using System.Threading;

namespace ProtocolDecoder
{
    class IsfDecoder
    {
        private static long DecodeText(string source, bool hasSummary)
        {
            string line = null;
            uint index = 0;
            Match match = null;
            StreamReader reader = new StreamReader(source);

            Item item = new Item(hasSummary);
            while ((line = reader.ReadLine()) != null)
            {
                index++;
                match = Regex.Match(line, @"^\d{4} .{6}  (..:..:..\....)  .{4}  0x(....)  (.*)");
                if (match.Success)
                {
                    item.Process();
                    item.Init(index, match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value);
                }
                else
                {
                    item.Add(line);
                }
            }
            item.Process();
            item.CloseFile();
            reader.Close();
            return item.GetApduCount();
        }

        public static void DecodeIsf(string sourceIsf, BackgroundWorker bw, bool usePCTime, bool needSummary, bool needMsg)
        {
            Utils.InitBaseFileName(sourceIsf);
            //FileInfo fileInfo = new FileInfo(isfFile);
            long totalCount = 0;

            if (!Utils.IsValidIsf(sourceIsf))
            {
                bw.ReportProgress(0, "choose valid qxdm log");
                return;
            }

            bw.ReportProgress(1, "opening qxdm");
            if (!QXDMProcessor.Start(sourceIsf))
            {
                bw.ReportProgress(0, "can't open qxdm");
                return;
            }

            bw.ReportProgress(1, "extracting isf log");

            LogMask mask = new LogMask(Utils.ExtractedFileName);
            mask.LogList = new uint[] { 0x1098, 0x14ce };
            if (needMsg)
            {
                mask.MsgList = new uint[] { 21, 6039};
                //PBM 6039
                //QCRIL 63
                //UIM 21
                //DS AT 5012
                //NAS 3006-3013 
                //CM 5
                //WMS 18
                //MMode 20
                //SD 15
                //MCFG 91
                //Policyman 99
                //CDMA 1007
                //UMTS 3000
                //DS UMTS 5008
                //DS 3GPP 5025
                mask.LogList = new uint[] { 0x1098, 0x14ce, //UIM APDU
                    0x1544, 0x138e, 0x138f, 0x1390, 0x1391, 0x1392, 0x1393, 0x1394, 0x1395, 0x1396, 0x1397, //QMI
                    0xb0c0, 0xb0e2, 0xb0e3, 0xb0ec, 0xb0ed, //OTA LTE
                    0x713a, 0x7b3a, 0xd0e3, 0x412f,         //OTA  UMTS, TDS, W
                    0x1004, 0x1005, 0x1006, 0x1007, 0x1008, //OTA 1X
                };
            }
            mask.DiagList = new uint[] { 124 };
            mask.SubSysList = new SubSysMask[] { new SubSysMask(8, 1), new SubSysMask(4, 15) };
            if (!QXDMProcessor.GetIsf(mask))
            {
                QXDMProcessor.Stop();
                bw.ReportProgress(0, "no valid log present.");
                return;
            }

            bw.ReportProgress(1, "opening apex/qcat");
            if (!IsfAnalyzer.Start(usePCTime))
            {
                bw.ReportProgress(0, "need to install apex or qcat");
                QXDMProcessor.Stop();
                return;
            }

            bw.ReportProgress(1, "saving isf log to text");
            if (!IsfAnalyzer.ConvetIsf2Text(Utils.ExtractedFileName, Utils.ExtractedTextName))
            {
                bw.ReportProgress(0, "no valid log present in extracted isf");
                IsfAnalyzer.Stop();
                QXDMProcessor.Stop();
                return;
            }
            QXDMProcessor.Stop();
            IsfAnalyzer.Stop();
            File.Delete(Utils.ExtractedFileName);

            bw.ReportProgress(1, "decoding text file");
            totalCount = DecodeText(Utils.ExtractedTextName, needSummary);
            //File.Delete(Utils.ExtractedTextName);

            string message = "total number of extracted apdu is " + totalCount;
            /*string extra = null;
            if (QXDMProcessor.IsBusy())
            {
                extra = ", still extracting qmi and ota log, do not close";
            }*/

            //bw.ReportProgress(1, message + extra);
            
            bw.ReportProgress(1, message);
        }
    }
}
