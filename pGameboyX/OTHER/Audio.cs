using System;
using PortAudioSharp;

namespace pGameboyX
{
    public class PortAudioX
    {
        private Stream _stream;
        private RingBuffer _ringBuffer;
        private uint _samplesPerFrame = 735;

        private Stream.Callback _callbackDelegate;
        public PortAudioX()
        {
#if Linux
        _samplesPerFrame = 512;
#elif Windows
         //Higher latency on windows, need more samples per frame. 1200 seems to be enough to not slow down emulation
         _samplesPerFrame = 1200; 
#endif
            _ringBuffer = new RingBuffer(4096);
            _callbackDelegate = new Stream.Callback(AudioCallback);
            

        }
        
        ~PortAudioX()
        {
          
            TerminateStream();
        }
        public void AddSample(short[] samples, int amount) { _ringBuffer.AddSample(samples, amount); }
        public int Count => _ringBuffer.Count;
        public uint SamplesPerFrame => _samplesPerFrame;
        
        public void Initialize()
        {
            PortAudio.Initialize();
            
            var streamParameters = new StreamParameters();
            var device = PortAudio.DefaultOutputDevice;
            PortAudioSharp.DeviceInfo data = PortAudio.GetDeviceInfo(device);
            streamParameters.channelCount = 1;
            streamParameters.device = device;
            streamParameters.sampleFormat = SampleFormat.Int16;
            PortAudioSharp.DeviceInfo deviceInfo = PortAudio.GetDeviceInfo(device);
            _stream = new Stream(null,streamParameters,44100, _samplesPerFrame, StreamFlags.NoFlag,_callbackDelegate,IntPtr.Zero);
        }



        public void Start()
        {
            if (_stream.IsStopped)
            {
                _stream.Start();
            }
        }


        public void Stop()
        {
            if (_stream.IsActive)
            {
                _stream.Stop();
            }
        }

        public void TerminateStream()
        {
            if(_stream != null)
            {
                if (_stream.IsActive)
                {
                    _stream.Stop();
                }
                _stream.Dispose();
            }
          
            PortAudio.Terminate();
        }

        private StreamCallbackResult AudioCallback(
            IntPtr input,
            IntPtr output,
            uint frameCount,
            ref StreamCallbackTimeInfo timeInfo,
            PortAudioSharp.StreamCallbackFlags statusFlags,
            IntPtr userData)
        {
            _ringBuffer.GetSamplesPointer((int)frameCount, output);
            return StreamCallbackResult.Continue;
        }
        
       

    }
     public class RingBuffer
        {

            private short[] _ringBuffer;
            private int _count = 0;
            private int _size = 0;
            private static readonly object obj = new object();

            


            private int tail = 0;
            private int head = 0;
            public RingBuffer(int size)
            {
                _size = size;
                _ringBuffer = new short[_size];
            }

            public int Count { get => _count; }

            public int AvailableSpace()
            {
                return _size - _count;
            }
            

            private void IncrementPointer(ref int pointer)
            {
                pointer += 1;
                pointer %= _size;
            }
            
            public void GetSamples(int count, ref short[] samples)
            {
                if (count > _count)
                {
                    //Console.WriteLine("Ran ouf of sound, left in buffer {0}", _count);
                    return;
                }
                lock (obj)
                {
                    for (int i = 0; i < count; i++)
                    {
                        samples[i] = _ringBuffer[tail];
                        _count--;
                        IncrementPointer(ref tail);
                    }
                }
            }
            
            public void GetSamplesPointer(int count, IntPtr buffer)
            {
            if (_count == 0) return;
            int amount = count > _count ? _count : count;
                lock (obj)
                {

                    unsafe
                    {
                    
                        short* ptr = (short*)buffer;
                        for (int i = 0; i < amount; i++)
                        {
                            *ptr= _ringBuffer[tail];
                            ptr++;
                            _count--;
                            IncrementPointer(ref tail);
                        }
                    }
                    
                }
            }

            public void AddSample(short[] samples, int amount)
            {

                if (amount > AvailableSpace()) return;

                lock (obj)
                {
                    for (int i = 0; i < amount; i++)
                    {
                        _ringBuffer[head] = samples[i];
                        _count++;
                        IncrementPointer(ref head);
                    }
                }
                
            }



        }
}
