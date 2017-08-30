using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace WXM.Media
{
    public class WxLame : IDisposable
    {
        public event EventHandler<double> OnProcess;

        private FileStream outStream;
 
        public int Size
        {
            get
            {
                return this.length;
            }
        }

        private ID3Tag tag;

        //uint = DWORD
        private uint hbeStream; 
        private uint bufferSize; 
        private uint dwSamples;

        private byte[] outBuffer;

        private int length;
        private bool disposed;
 
        /// <summary>
        /// エンコーダを初期化します。
        /// </summary>
        /// <param name="samplingRate">サンプリングレート 32000 or 44100 or 48000</param>
        /// <param name="channels">チャンネル数 1 or 2 </param>
        /// <param name="bitRateInKBPS">ビットレート (kBps) 128 とか 192 とか 320 とか</param>
        public WxLame(uint samplingRate, uint channels, uint bitRateInKBPS, int preset, ID3Tag id3 = null)
        { 
            this.tag = id3;

            // Init the MP3 Stream
            NativeLame.BECONFIG beConfig = new NativeLame.BECONFIG();

            // use the LAME config structure
            beConfig.dwConfig = 256u;

            beConfig.format.LHV1.dwStructVersion = 1u;
            beConfig.format.LHV1.dwStructSize = (uint)Marshal.SizeOf(beConfig);
            beConfig.format.LHV1.dwSampleRate = samplingRate;              // INPUT FREQUENCY
            beConfig.format.LHV1.dwReSampleRate = 0;                    // DON"T RESAMPLE
            beConfig.format.LHV1.nMode = channels == 2u ? 1u : 3u;   // OUTPUT IN STREO
            beConfig.format.LHV1.dwBitrate = bitRateInKBPS;                   // MINIMUM BIT RATE
            beConfig.format.LHV1.nPreset = preset;       // QUALITY PRESET SETTING
                       
            beConfig.format.LHV1.bOriginal = 1;                  // SET ORIGINAL FLAG
            beConfig.format.LHV1.bWriteVBRHeader = 1;                    // Write INFO tag

            beConfig.format.LHV1.dwMaxBitrate		= 320u;					// MAXIMUM BIT RATE
            beConfig.format.LHV1.bCRC				= 0;					// INSERT CRC
            beConfig.format.LHV1.bCopyright			= 0;					// SET COPYRIGHT FLAG	
            beConfig.format.LHV1.bPrivate			= 1;					// SET PRIVATE FLAG
            beConfig.format.LHV1.bWriteVBRHeader	= 0;					// YES, WRITE THE XING VBR HEADER
            beConfig.format.LHV1.bEnableVBR			= 1;					// USE VBR
            beConfig.format.LHV1.nVBRQuality		= 2;            // SET VBR QUALITY
            beConfig.format.LHV1.nQuality           = 2;
            beConfig.format.LHV1.nVbrMethod = 3;
            beConfig.format.LHV1.dwVbrAbr_bps = bitRateInKBPS * 1000;

            beConfig.format.LHV1.bNoBitRes = 1;                 // No Bit resorvoir
 
            uint r = NativeLame.beInitStream(ref beConfig, out dwSamples, out bufferSize, out hbeStream);

            if (r != 0)
                throw new Exception("Lameの初期化に失敗したっす。(" + r + ")"); 
        }

        // Close LAME instance and output stream on dispose
        /// <summary>Dispose of object</summary>
        /// <param name="final">True if called from destructor, false otherwise</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.hbeStream = 0;
                }

                this.disposed = true; 

                this.outBuffer = null;
                this.outStream = null;       
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Encode PCM to MP3.
        /// エンコードする。
        /// </summary>
        /// <param name="waveformStream">waveform</param>
        /// <returns>mp3chunks</returns>
        /// <remarks>
        /// 入力されるデータは SamplesPerChunk 単位になる。
        /// 単位に満たなかったサンプルはEncoder内部でバッファされ、次のEncode時に使われる。
        /// </remarks>
        public void Encode(string wavfile, string mp3file)
        {
            if (hbeStream == 0)
                throw new InvalidOperationException("開いてないんだけど！");
            
            this.outBuffer = new byte[this.bufferSize * 8];
            byte[] inBuffer = new byte[dwSamples * 8];

            //short = int16
            uint count = 0;
            double total = 0;

            FileStream fs = new FileStream(wavfile, FileMode.Open);

            try
            {
                this.outStream = File.Create(mp3file);
                
                this.length = (int)fs.Length;

                //skip wav header

                fs.Seek(44, SeekOrigin.Begin);

                while (total < this.length - 1)
                { 
                    int dwRead = fs.Read(inBuffer, 0, inBuffer.Length);

                    if (dwRead > 0)
                    {
                        NativeLame.beEncodeChunk(this.hbeStream, (uint)dwRead / 2, inBuffer, this.outBuffer, out count);

                        if (count > 0)
                            this.outStream.Write(this.outBuffer, 0, (int)count);

                        total += dwRead;

                        if (OnProcess != null)
                        { 
                            OnProcess(this, total / length);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
 
                uint err = NativeLame.beDeinitStream(this.hbeStream, this.outBuffer, out count);

                if (err == 0)
                { 
                    this.outStream.Write(this.outBuffer, 0, (int)count);
                   
                }
            }
            catch(Exception ex)
            { 
                throw ex;
            }
            finally
            {
                this.outStream.Flush();
                this.outStream.Close();

                fs.Close();

                NativeLame.beCloseStream(hbeStream);               
            }
        }
        
        /// <summary>
        /// Lame_enc.dll ラッピングクラス
        /// </summary>
        internal static class NativeLame
        {
            public static bool Available
            {
                get { return System.IO.File.Exists("lame_enc.dll"); }
            }

            public const string libname = "lame_enc.dll";

            [DllImport(libname, CharSet = CharSet.Ansi)]
            public static extern uint beInitStream(ref BECONFIG pbeConfig, out uint dwSamples, out uint dwBufferSize, out uint phbeStream);

            [DllImport(libname, CharSet = CharSet.Ansi)]
            public static extern uint beEncodeChunk(uint hbeStream, uint nSamples, byte[] pSamples, [In, Out] byte[] pOutput, out uint pdwOutput);

            [DllImport(libname, CharSet = CharSet.Ansi)]
            public static extern uint beDeinitStream(uint hbeStream, [In, Out] byte[] pOutput, out uint pdwOutput);

            [DllImport(libname, CharSet = CharSet.Ansi)]
            internal static extern uint beFlushNoGap(uint hbeStream, [In, Out] byte[] pOutput, out uint pdwOutput);

            [DllImport(libname, CharSet = CharSet.Ansi)]
            public static extern uint beCloseStream(uint hbeStream);

            [DllImport(libname, CharSet = CharSet.Ansi)]
            public static extern uint beWriteVBRHeader(string filename);

            [DllImport(libname, CharSet = CharSet.Ansi)]
            public static extern uint beWriteInfoTag(uint hbeStream, string filename);

            public struct LHV1
            {
                public uint dwStructVersion;
                public uint dwStructSize;

                // BASIC ENCODER SETTINGS
                public uint dwSampleRate;       // SAMPLERATE OF INPUT FILE
                public uint dwReSampleRate;     // DOWNSAMPLERATE, 0=ENCODER DECIDES  
                public uint nMode;              // STEREO, MONO
                public uint dwBitrate;          // CBR bitrate, VBR min bitrate
                public uint dwMaxBitrate;       // CBR ignored, VBR Max bitrate
                public int nPreset;         // Quality preset

                // BIT STREAM SETTINGS
                public int bCopyright;          // Set Copyright Bit (TRUE/FALSE)
                public int bCRC;                // Insert CRC (TRUE/FALSE)                    
                public int bOriginal;
                public int bPrivate;            // Set Private Bit (TRUE/FALSE)

                // VBR STUFF
                public int nVbrMethod;
                public int bWriteVBRHeader; // WRITE XING VBR HEADER (TRUE/FALSE)
                public int bEnableVBR;          // USE VBR ENCODING (TRUE/FALSE)
                public int nVBRQuality;     // VBR QUALITY 0..9
                public uint dwVbrAbr_bps;       // Use ABR in stead of nVBRQuality

                public int bNoBitRes;              // Disable Bit resorvoir (TRUE/FALSE)
                public ushort nQuality;
            }

            public struct format
            {
                public LHV1 LHV1;
            }

            [StructLayout(LayoutKind.Sequential, Size = 331), Serializable]
            public struct BECONFIG // BE_CONFIG_LAME LAME header version 1
            {
                // STRUCTURE INFORMATION
                public uint dwConfig;

                public format format;
            }
        }
    }
}
