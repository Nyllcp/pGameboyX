using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using System;
using System.IO;
using System.Threading;
using Avalonia.Input;
using Avalonia.Media.Imaging;

namespace pGameboyX;

public partial class MainWindow : Window
{
        private PortAudioX _audio;
        private IO _io;
        private Core? _gb;

    
        const byte gbWidth = 160;
        const byte gbHeigth = 144;

        private Thread? _emulationThread;
        private Thread? _renderThread;
        private volatile bool _run;

        private readonly object _frameLock = new();
        private uint[] _frameBuffer = new uint[gbWidth * gbHeigth];
        
        private IStorageFolder? _lastFolder;
        private string _currentRomPath = "";



        public MainWindow()
        {
            InitializeComponent();
            _audio = new PortAudioX();
            _io = new IO();
            Opened += OnOpened;
            Closing += OnClosing;
            InputElement.KeyDownEvent.AddClassHandler<TopLevel>(OnKeyDown);
            InputElement.KeyUpEvent.AddClassHandler<TopLevel>(OnKeyUp);
            
            
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if(_gb != null) _io.AvaloniaKeyDown(ref _gb, e);
            e.Handled = true;
        }
        private void OnKeyUp(object? sender, KeyEventArgs e)
        {
            if(_gb != null) _io.AvaloniaKeyUp(ref _gb, e);
            e.Handled = true;
        }
        private void OnOpened(object? sender, EventArgs e)
        {

            _audio.Initialize();
            RomNameText.Text = "No Rom Loaded";
            StateText.Text = "Selected State : null";
            FpsText.Text = "Emulator FPS:";
            FpsRedrawText.Text = "Blit FPS:";

            //using var stream = File.OpenRead("pNesX_title.bmp"); 
            //GameView.SetBitmapFromStream(stream);

        }

        private void OnClosing(object? sender, WindowClosingEventArgs e)
        {
           
            StopEmulation();
            _audio.TerminateStream();
            if (_gb != null)
                _gb.WriteSave();
        }


        private async void Open_Click(object? sender, RoutedEventArgs e)
        {
            
            
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null)
                return;

            _lastFolder = await topLevel.StorageProvider
                .TryGetFolderFromPathAsync(Environment.CurrentDirectory);
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = "Open Gameboy/Gameboy Color ROM",
                    AllowMultiple = false,
                    SuggestedStartLocation = _lastFolder,
                    FileTypeFilter = [ new FilePickerFileType("GB/GBC ROMs")
                    {
                        Patterns = [ "*.gb","*.gbc","*.GB","*.GBC", "*.zip" ]
                    }]
      
                });

            if (files.Count == 0)
                return;
            _lastFolder = await files[0].GetParentAsync();
            
            StopEmulation();
            
            var file = files[0];
            
            _currentRomPath= file.Path.LocalPath;
       
            
            _gb = new Core();

            if (!_gb.LoadRom(_currentRomPath))
            {
                RomNameText.Text = "Invalid file";
                return;
            }
            RomNameText.Text = $"{_gb.CurrentRomName}";
            StartEmulation();
        }

        private void Reset_Click(object? sender, RoutedEventArgs e)
        {
            if (_gb == null)
                return;

            StopEmulation();

            _gb.WriteSave();
            _gb = new Core();

            if (_gb.LoadRom(_currentRomPath))
                StartEmulation();
        }


        private void Exit_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }
        
        private void StartEmulation()
        {
            _run = true;
            _audio.Start();
            if (_emulationThread != null && _emulationThread.IsAlive)
                return;
            if (_renderThread != null && _renderThread.IsAlive)
                return;

            _emulationThread = new Thread(EmulationThread)
            {
                IsBackground = true,
                Name = "NES Emulation Thread"
            };

            _emulationThread.Start();

            _renderThread = new Thread(RenderThread)
            {
                IsBackground = true,
                Name = "NES Render Thread"
            };

            _renderThread.Start();
           

        }


        private void StopEmulation()
        {
            _run = false;
            _audio.Stop();
            if (_emulationThread != null &&
                _emulationThread.IsAlive)
            {
                _emulationThread.Join();
            }
            if (_renderThread != null &&
                _renderThread.IsAlive)
            {
                _renderThread.Join();
            }
   
        }


        private void EmulationThread()
        {
            int frames = 0;
            long lastTime = _io.ElapsedTimeMS();

            while (_run)
            {
                if (_io.FrameLimit)
                {
                    while (_audio.Count > _audio.SamplesPerFrame * 2 && _run)
                    {
                        // spin

                    }
                }
                
                _gb.RunOneFrame();
                
                _audio.AddSample(
                    _gb.GetSamples,
                    _gb.NumberOfSamples
                );
                
                lock (_frameLock)
                {
                    _frameBuffer = _gb.FrambufferRGB;

                }
                frames++;
                var now = _io.ElapsedTimeMS();
                if (now - lastTime > 1000)
                {
                    lastTime = now;
                    var fps = frames;
                    frames = 0;

                    Dispatcher.UIThread.Post(() =>
                    {
                        FpsText.Text = $"Emulated FPS: {fps}";

                    });
                }
            }
        }

        private void RenderThread()
        {
            uint[] frame;
            long renderLimiter = _io.ElapsedTimeMicro();
            long lastTime = _io.ElapsedTimeMS();
            while (_run)
            {

            

                var millisNow = _io.ElapsedTimeMicro();
                if(millisNow - renderLimiter > 16667)
                {
                    //60 fps
                    renderLimiter = millisNow;
                    lock (_frameLock)
                    {
                        frame = new uint[_frameBuffer.Length];
                        _frameBuffer.CopyTo(frame, 0);

                    }
                    Dispatcher.UIThread.Post(() =>
                    {
                        GameView.UpdateFrame(frame);
                        StateText.Text = $"Selected State : {_gb.SelectedSavestate}";

                    });
                }


                var now = _io.ElapsedTimeMS();
                if (now - lastTime > 1000)
                {
                    lastTime = now;
                    var redrawFps = GameView.redrawFrames;
                    GameView.redrawFrames = 0;

                    Dispatcher.UIThread.Post(() =>
                    {
                        FpsRedrawText.Text = $"Blit FPS: {redrawFps}";

                    });
                }
               
            }
        }

        private void Interpolation_OnClick(object? sender, RoutedEventArgs e)
        {
            if(sender == null) return;
            var tag = ((MenuItem)sender).Tag;

            switch (tag)
            {
                case "None": GameView.InterpolationMode(BitmapInterpolationMode.None); break;
                case "Low": GameView.InterpolationMode(BitmapInterpolationMode.LowQuality); break;
                case "High": GameView.InterpolationMode(BitmapInterpolationMode.HighQuality); break;
            }
  
        }
}