﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Path = System.IO.Path;

namespace NotEnoughEncodes
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            //Get Number of Cores
            int coreCount = 0;
            foreach (var item in new System.Management.ManagementObjectSearcher("Select * from Win32_Processor").Get())
            {
                coreCount += int.Parse(item["NumberOfCores"].ToString());
            }
            //Sets the Number of Workers = Phsyical Core Count
            int tempCorecount = coreCount * 1 / 2;
            TextBoxNumberWorkers.Text = tempCorecount.ToString();

            //Load Settings
            readSettings();

            //If settings.ini exist -> Set all Values
            bool fileExist = File.Exists("encoded.txt");

            if (fileExist)
            {
                MessageBox.Show("May have detected unfished / uncleared Encode. If you want to resume an unfinished Job, check the Checkbox " + '\u0022' + "Resume" + '\u0022');
            }
        }

        public void readSettings()
        {
            try
            {
                //If settings.ini exist -> Set all Values
                bool fileExist = File.Exists("settings.ini");

                if (fileExist)
                {
                    string[] lines = System.IO.File.ReadAllLines("settings.ini");

                    TextBoxNumberWorkers.Text = lines[0];
                    ComboBoxCpuUsed.Text = lines[1];
                    ComboBoxBitdepth.Text = lines[2];
                    TextBoxEncThreads.Text = lines[3];
                    TextBoxcqLevel.Text = lines[4];
                    TextBoxKeyframeInterval.Text = lines[5];
                    TextBoxTileCols.Text = lines[6];
                    TextBoxTileRows.Text = lines[7];
                    ComboBoxPasses.Text = lines[8];
                    TextBoxFramerate.Text = lines[9];
                    ComboBoxEncMode.Text = lines[10];
                    TextBoxChunkLength.Text = lines[11];
                }
                //Reads custom settings to settings_custom.ini
                bool customFileExist = File.Exists("settings_custom.ini");
                if (customFileExist)
                {
                    string[] linesa = System.IO.File.ReadAllLines("settings_custom.ini");
                    TextBoxCustomSettings.Text = linesa[0];
                }
            }
            catch { }
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            //Saves all Current Settings to a file
            string maxConcurrency = TextBoxNumberWorkers.Text;
            string cpuUsed = ComboBoxCpuUsed.Text;
            string bitDepth = ComboBoxBitdepth.Text;
            string encThreads = TextBoxEncThreads.Text;
            string cqLevel = TextBoxcqLevel.Text;
            string kfmaxdist = TextBoxKeyframeInterval.Text;
            string tilecols = TextBoxTileCols.Text;
            string tilerows = TextBoxTileRows.Text;
            string nrPasses = ComboBoxPasses.Text;
            string fps = TextBoxFramerate.Text;
            string encMode = this.ComboBoxEncMode.Text;
            string chunkLength = TextBoxChunkLength.Text;
            string customSettings = TextBoxCustomSettings.Text;

            //Saves custom settings in settings_custom.ini
            if (CheckBoxCustomSettings.IsChecked == true)
            {
                string[] linescustom = { customSettings };
                System.IO.File.WriteAllLines("settings_custom.ini", linescustom);
            }

            string[] lines = { maxConcurrency, cpuUsed, bitDepth, encThreads, cqLevel, kfmaxdist, tilecols, tilerows, nrPasses, fps, encMode, chunkLength };
            System.IO.File.WriteAllLines("settings.ini", lines);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //Open File Dialog
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
                TextBoxInputVideo.Text = openFileDialog.FileName;

            //If ffprobe exist -> Set all Values
            bool fileExist = File.Exists("ffprobe.exe");

            //Gets the Stream Framerate IF ffrpobe exist
            if (fileExist)
            {
                getStreamFps(TextBoxInputVideo.Text);
            }
        }

        private void ButtonOutput_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Matroska|*.mkv";
            if (saveFileDialog.ShowDialog() == true)
                TextBoxOutputVideo.Text = saveFileDialog.FileName;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (Cancel.CancelAll == true && CheckBoxResume.IsChecked == false)
            {
                //Asks the user if he wants to resume the process.
                if (MessageBox.Show("It appears that you canceled a previous encode. If you want to resume an cancelled encode, press Yes.",
                    "Resume", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    CheckBoxResume.IsChecked = true;
                    Cancel.CancelAll = false;
                    pLabel.Dispatcher.Invoke(() => pLabel.Content = "Resuming...", DispatcherPriority.Background);
                    prgbar.Maximum = 100;
                    prgbar.Value = 0;
                }
                else
                {
                    Cancel.CancelAll = false;
                    pLabel.Dispatcher.Invoke(() => pLabel.Content = "Starting...", DispatcherPriority.Background);
                    prgbar.Maximum = 100;
                    prgbar.Value = 0;
                }
            }
            else if (Cancel.CancelAll == true && CheckBoxResume.IsChecked == true)
            {
                Cancel.CancelAll = false;
                pLabel.Dispatcher.Invoke(() => pLabel.Content = "Resuming...", DispatcherPriority.Background);
                prgbar.Maximum = 100;
                prgbar.Value = 0;
            }

            if (CheckBoxLogging.IsChecked == true)
            {
                WriteToFileThreadSafe(DateTime.Now.ToString("h:mm:ss tt") + " Clicked on Start Encode", "log.log");
            }

            if (TextBoxInputVideo.Text == " Input Video")
            {
                MessageBox.Show("No Input File selected!");

                if (CheckBoxLogging.IsChecked == true)
                {
                    WriteToFileThreadSafe(DateTime.Now.ToString("h:mm:ss tt") + " No Input File selected!", "log.log");
                }
            }
            else if (TextBoxOutputVideo.Text == " Output Video")
            {
                MessageBox.Show("No Output Path specified!");

                if (CheckBoxLogging.IsChecked == true)
                {
                    WriteToFileThreadSafe(DateTime.Now.ToString("h:mm:ss tt") + " No Output Path specified!", "log.log");
                }
            }
            else if (TextBoxInputVideo.Text != " Input Video")
            {
                if (CheckBoxLogging.IsChecked == true)
                {
                    WriteToFileThreadSafe(DateTime.Now.ToString("h:mm:ss tt") + " Started MainClass()", "log.log");
                }
                //Start MainClass
                MainClass();
            }
        }

        public static class Cancel
        {
            //Public Cancel boolean
            public static bool CancelAll = false;
        }

        public void MainClass()
        {
            if (CheckBoxLogging.IsChecked == true)
            {
                WriteToFileThreadSafe(DateTime.Now.ToString("h:mm:ss tt") + " MainClass started", "log.log");
            }
            //Sets Label
            pLabel.Dispatcher.Invoke(() => pLabel.Content = "Starting...", DispatcherPriority.Background);

            //Sets the working directory
            string currentPath = Directory.GetCurrentDirectory();
            //Checks if Chunks folder exist, if no it creates Chunks folder
            if (!Directory.Exists(Path.Combine(currentPath, "Chunks")))
                Directory.CreateDirectory(Path.Combine(currentPath, "Chunks"));
            if (CheckBoxLogging.IsChecked == true)
            {
                WriteToFileThreadSafe(DateTime.Now.ToString("h:mm:ss tt") + " Checked or Created Chunks folder", "log.log");
            }

            //Sets the variable for input / output of video
            string videoInput = TextBoxInputVideo.Text;
            string videoOutput = TextBoxOutputVideo.Text;

            //Start Splitting
            if (CheckBoxResume.IsChecked == false)
            {
                if (CheckBoxLogging.IsChecked == true)
                {
                    WriteToFileThreadSafe(DateTime.Now.ToString("h:mm:ss tt") + " Start Splitting", "log.log");
                }

                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = "cmd.exe";
                //FFmpeg Arguments

                //Checks if Source needs to be reencoded
                if (CheckBoxReencode.IsChecked == false)
                {
                    if (CheckBoxLogging.IsChecked == true)
                    {
                        WriteToFileThreadSafe(DateTime.Now.ToString("h:mm:ss tt") + " Splitting without reencoding", "log.log");
                    }
                    startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + videoInput + '\u0022' + " -vcodec copy -f segment -segment_time " + TextBoxChunkLength.Text + " -an " + '\u0022' + "Chunks\\out%0d.mkv" + '\u0022';
                }
                else if (CheckBoxReencode.IsChecked == true)
                {
                    if (CheckBoxLogging.IsChecked == true)
                    {
                        WriteToFileThreadSafe(DateTime.Now.ToString("h:mm:ss tt") + " Splitting with reencoding", "log.log");
                    }
                    startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + videoInput + '\u0022' + " -c:v utvideo -f segment -segment_time " + TextBoxChunkLength.Text + " -an " + '\u0022' + "Chunks\\out%0d.mkv" + '\u0022';
                }
                //Console.WriteLine(startInfo.Arguments);
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
            }

            //Audio Encoding
            if (CheckBoxEnableAudio.IsChecked == true && CheckBoxResume.IsChecked == false)
            {
                encodeAudio(videoInput);
            }

            //Create Array List with all Chunks
            string[] chunks;
            //Sets the Chunks directory
            string sdira = currentPath + "\\Chunks";
            //Add all Files in Chunks Folder to array
            chunks = Directory.GetFiles(sdira, "*mkv", SearchOption.AllDirectories).Select(x => Path.GetFileName(x)).ToArray();

            if (CheckBoxResume.IsChecked == false)
            {
                DirectoryInfo d = new DirectoryInfo(currentPath + "\\Chunks");
                FileInfo[] infos = d.GetFiles();

                int numberOfChunks = chunks.Count();

                //outx.mkv = 8 | outxx.mkv = 9 (99) | outxxx.mkv = 10 (999) | outxxxx.mkv = 11 (9999) | outxxxxx.mkv = 12 (99999)

                //int numberOfChunks = 20000;

                if (numberOfChunks >= 10 && numberOfChunks <= 99)
                {
                    foreach (FileInfo f in infos)
                    {
                        int count = f.ToString().Count();

                        if (count == 8)
                        {
                            System.IO.File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out0"));
                        }
                    }
                }
                else if (numberOfChunks >= 100 && numberOfChunks <= 999) //If you have more than 100 Chunks and less than 999
                {
                    foreach (FileInfo f in infos)
                    {
                        int count = f.ToString().Count();

                        if (count == 8)
                        {
                            System.IO.File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out00"));
                        }

                        if (count == 9)
                        {
                            System.IO.File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out0"));
                        }
                    }
                }
                else if (numberOfChunks >= 1000 && numberOfChunks <= 9999) //If you have more than 1.000 Chunks and less than 9.999
                {
                    foreach (FileInfo f in infos)
                    {
                        int count = f.ToString().Count();

                        if (count == 8)
                        {
                            System.IO.File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out000"));
                        }

                        if (count == 9)
                        {
                            System.IO.File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out00"));
                        }

                        if (count == 10)
                        {
                            System.IO.File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out0"));
                        }
                    }
                }
                else if (numberOfChunks >= 10000 && numberOfChunks <= 99999) //If you have more than 10.000 Chunks and less than 99.999
                {
                    foreach (FileInfo f in infos)
                    {
                        int count = f.ToString().Count();

                        if (count == 8)
                        {
                            System.IO.File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out0000"));
                        }

                        if (count == 9)
                        {
                            System.IO.File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out000"));
                        }

                        if (count == 10)
                        {
                            System.IO.File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out00"));
                        }

                        if (count == 11)
                        {
                            System.IO.File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out0"));
                        }
                    }
                }
                else if (numberOfChunks >= 100000 && numberOfChunks <= 999999)
                {
                    foreach (FileInfo f in infos)
                    {
                        int count = f.ToString().Count();
                        //If you have more than 100.000 Chunks and less than 999.999
                        //BTW are fu*** insane?
                        if (count == 8)
                        {
                            System.IO.File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out00000"));
                        }

                        if (count == 9)
                        {
                            System.IO.File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out0000"));
                        }

                        if (count == 10)
                        {
                            System.IO.File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out000"));
                        }

                        if (count == 11)
                        {
                            System.IO.File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out00"));
                        }
                        if (count == 12)
                        {
                            System.IO.File.Move(d + "\\" + f, d + "\\" + f.Name.Replace("out", "out0"));
                        }
                    }
                }
            }

            //Parse Textbox Text to String for loop threading
            int maxConcurrency = Int16.Parse(TextBoxNumberWorkers.Text);
            int cpuUsed = Int16.Parse(ComboBoxCpuUsed.Text);
            int bitDepth = Int16.Parse(ComboBoxBitdepth.Text);
            int encThreads = Int16.Parse(TextBoxEncThreads.Text);
            int cqLevel = Int16.Parse(TextBoxcqLevel.Text);
            int kfmaxdist = Int16.Parse(TextBoxKeyframeInterval.Text);
            int tilecols = Int16.Parse(TextBoxTileCols.Text);
            int tilerows = Int16.Parse(TextBoxTileRows.Text);
            int nrPasses = Int16.Parse(ComboBoxPasses.Text);
            string fps = TextBoxFramerate.Text;
            string encMode = this.ComboBoxEncMode.Text;
            bool resume = false;

            //Sets Resume Mode
            if (CheckBoxResume.IsChecked == true)
            {
                resume = true;
                if (CheckBoxLogging.IsChecked == true)
                {
                    WriteToFileThreadSafe(DateTime.Now.ToString("h:mm:ss tt") + " Resume Mode Started", "log.log");
                }

                foreach (string line in File.ReadLines("encoded.txt"))
                {
                    //Removes all Items from Arraylist which are in encoded.txt
                    chunks = chunks.Where(s => s != line).ToArray();
                    if (CheckBoxLogging.IsChecked == true)
                    {
                        WriteToFileThreadSafe(DateTime.Now.ToString("h:mm:ss tt") + " Resume Mode - Deleting " + line + " from Array", "log.log");
                    }
                }
                //Set the Maximum Value of Progressbar
                prgbar.Maximum = chunks.Count();
            }

            string finalEncodeMode = "";

            //Sets the Encoding Mode
            if (encMode == "q")
            {
                if (CheckBoxLogging.IsChecked == true)
                {
                    WriteToFileThreadSafe(DateTime.Now.ToString("h:mm:ss tt") + " Set Encode Mode to q", "log.log");
                }
                finalEncodeMode = " --end-usage=q --cq-level=" + cqLevel;
            }
            else if (encMode == "vbr")
            {
                //If vbr set finalEncodeMode
                if (CheckBoxLogging.IsChecked == true)
                {
                    WriteToFileThreadSafe(DateTime.Now.ToString("h:mm:ss tt") + " Set Encode Mode to vbr", "log.log");
                }
                finalEncodeMode = " --end-usage=vbr --target-bitrate=" + cqLevel;
            }
            else if (encMode == "cbr")
            {
                //If cbr set finalEncodeMode
                if (CheckBoxLogging.IsChecked == true)
                {
                    WriteToFileThreadSafe(DateTime.Now.ToString("h:mm:ss tt") + " Set Encode Mode to cbr", "log.log");
                }
                finalEncodeMode = " --end-usage=cbr --target-bitrate=" + cqLevel;
            }

            string allSettingsAom = "";
            //Sets aom settings to custom or preset
            if (CheckBoxCustomSettings.IsChecked == true)
            {
                allSettingsAom = " " + TextBoxCustomSettings.Text;
                //Console.WriteLine(allSettingsAom);
            }
            else if (CheckBoxCustomSettings.IsChecked == false)
            {
                allSettingsAom = " --cpu-used=" + cpuUsed + " --threads=" + encThreads + finalEncodeMode + " --bit-depth=" + bitDepth + " --tile-columns=" + tilecols + " --fps=" + fps + " --tile-rows=" + tilerows + " --kf-max-dist=" + kfmaxdist;
                //Console.WriteLine(allSettingsAom);
            }

            //Sets the boolean if audio should be included -> Concat() needs this value
            bool audioOutput = false;
            if (CheckBoxEnableAudio.IsChecked == true)
            {
                if (CheckBoxLogging.IsChecked == true)
                {
                    WriteToFileThreadSafe(DateTime.Now.ToString("h:mm:ss tt") + " Set Audio Boolean to true", "log.log");
                }
                audioOutput = true;
            }

            //Starts the async task
            StartTask(maxConcurrency, nrPasses, allSettingsAom, resume, videoOutput, audioOutput);
            //Set Maximum of Progressbar
            prgbar.Maximum = chunks.Count();
            //Set the Progresslabel to 0 out of Number of chunks, because people would think that it doesnt to anything
            pLabel.Dispatcher.Invoke(() => pLabel.Content = "0 / " + prgbar.Maximum, DispatcherPriority.Background);
        }

        //Async Class -> UI doesnt freeze
        private async void StartTask(int maxConcurrency, int passes, string allSettingsAom, bool resume, string videoOutput, bool audioOutput)
        {
            if (CheckBoxLogging.IsChecked == true)
            {
                WriteToFileThreadSafe(DateTime.Now.ToString("h:mm:ss tt") + " Async Task started", "log.log");
            }
            //Run encode class async
            await Task.Run(() => encode(maxConcurrency, passes, allSettingsAom, resume, videoOutput, audioOutput));
        }

        //Main Encoding Class
        public void encode(int maxConcurrency, int passes, string allSettingsAom, bool resume, string videoOutput, bool audioOutput)
        {
            //Set Working directory
            string currentPath = Directory.GetCurrentDirectory();

            //Create Array List with all Chunks
            string[] chunks;
            //Sets the Chunks directory
            string sdira = currentPath + "\\Chunks";

            //Add all Files in Chunks Folder to array
            chunks = Directory.GetFiles(sdira, "*mkv", SearchOption.AllDirectories).Select(x => Path.GetFileName(x)).ToArray();

            if (resume == true)
            {
                //Removes all Items from Arraylist which are in encoded.txt
                foreach (string line in File.ReadLines("encoded.txt"))
                {
                    chunks = chunks.Where(s => s != line).ToArray();
                }
            }

            //Get Number of chunks for label of progressbar
            string labelstring = chunks.Count().ToString();

            //Parallel Encoding - aka some blackmagic
            using (SemaphoreSlim concurrencySemaphore = new SemaphoreSlim(maxConcurrency))
            {
                List<Task> tasks = new List<Task>();
                foreach (var items in chunks)
                {
                    concurrencySemaphore.Wait();

                    var t = Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            if (Cancel.CancelAll == false)
                            {
                                if (passes == 1)
                                {
                                    System.Diagnostics.Process process = new System.Diagnostics.Process();
                                    System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                                    startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                                    startInfo.FileName = "cmd.exe";
                                    startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + sdira + "\\" + items + '\u0022' + " -pix_fmt yuv420p -vsync 0 -f yuv4mpegpipe - | aomenc.exe - --passes=1" + allSettingsAom + " --output=Chunks\\" + items + "-av1.ivf";
                                    process.StartInfo = startInfo;
                                    Console.WriteLine(startInfo.Arguments);
                                    process.Start();
                                    process.WaitForExit();

                                        //Progressbar +1
                                        prgbar.Dispatcher.Invoke(() => prgbar.Value += 1, DispatcherPriority.Background);
                                        //Label of Progressbar = Progressbar
                                        pLabel.Dispatcher.Invoke(() => pLabel.Content = prgbar.Value + " / " + labelstring, DispatcherPriority.Background);
                                    if (Cancel.CancelAll == false)
                                    {
                                            //Write Item to file for later resume if something bad happens
                                            WriteToFileThreadSafe(items, "encoded.txt");
                                    }
                                    else
                                    {
                                        KillInstances();
                                    }
                                }
                                else if (passes == 2)
                                {
                                    System.Diagnostics.Process process = new System.Diagnostics.Process();
                                    System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                                    startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                                    startInfo.FileName = "cmd.exe";
                                    startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + sdira + "\\" + items + '\u0022' + " -pix_fmt yuv420p -vsync 0 -f yuv4mpegpipe - | aomenc.exe - --passes=2 --pass=1 --fpf=Chunks\\" + items + "_stats.log" + allSettingsAom + " --output=NUL";
                                    process.StartInfo = startInfo;
                                        //Console.WriteLine(startInfo.Arguments);
                                        process.Start();
                                    process.WaitForExit();

                                    startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                                    startInfo.FileName = "cmd.exe";
                                    startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + sdira + "\\" + items + '\u0022' + " -pix_fmt yuv420p -vsync 0 -f yuv4mpegpipe - | aomenc.exe - --passes=2 --pass=2 --fpf=Chunks\\" + items + "_stats.log" + allSettingsAom + " --output=Chunks\\" + items + "-av1.ivf";
                                    process.StartInfo = startInfo;
                                        //Console.WriteLine(startInfo.Arguments);
                                        process.Start();
                                    process.WaitForExit();

                                    prgbar.Dispatcher.Invoke(() => prgbar.Value += 1, DispatcherPriority.Background);
                                    pLabel.Dispatcher.Invoke(() => pLabel.Content = prgbar.Value + " / " + labelstring, DispatcherPriority.Background);
                                    if (Cancel.CancelAll == false)
                                    {
                                            //Write Item to file for later resume if something bad happens
                                            WriteToFileThreadSafe(items, "encoded.txt");
                                    }
                                    else
                                    {
                                        KillInstances();
                                    }
                                }
                            }
                        }
                        finally
                        {
                            concurrencySemaphore.Release();
                        }
                    });

                    tasks.Add(t);
                }

                Task.WaitAll(tasks.ToArray());
            }

            //Mux all Encoded chunks back together
            concat(videoOutput, audioOutput);
        }

        //Mux ivf Files back together
        private void concat(string videoOutput, bool audioOutput)
        {
            if (Cancel.CancelAll == false)
            {
                string currentPath = Directory.GetCurrentDirectory();

                string outputfilename = videoOutput;

                pLabel.Dispatcher.Invoke(() => pLabel.Content = "Muxing files", DispatcherPriority.Background);

                //Lists all ivf files in mylist.txt
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = "cmd.exe";
                //FFmpeg Arguments

                startInfo.Arguments = "/C (for %i in (Chunks\\*.ivf) do @echo file '%i') > Chunks\\mylist.txt";
                //Console.WriteLine(startInfo.Arguments);
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();

                if (audioOutput == false)
                {
                    //Concat the Videos
                    startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    startInfo.FileName = "cmd.exe";
                    //FFmpeg Arguments

                    startInfo.Arguments = "/C ffmpeg.exe -f concat -safe 0 -i Chunks\\mylist.txt -c copy " + '\u0022' + outputfilename + '\u0022';
                    //Console.WriteLine(startInfo.Arguments);
                    process.StartInfo = startInfo;
                    process.Start();
                    process.WaitForExit();
                }
                else if (audioOutput == true)
                {
                    //Concat the Videos
                    startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    startInfo.FileName = "cmd.exe";
                    //FFmpeg Arguments

                    startInfo.Arguments = "/C ffmpeg.exe -f concat -safe 0 -i Chunks\\mylist.txt -c copy no_audio.mkv";
                    //Console.WriteLine(startInfo.Arguments);
                    process.StartInfo = startInfo;
                    process.Start();
                    process.WaitForExit();

                    //Concat the Videos
                    startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    startInfo.FileName = "cmd.exe";
                    //FFmpeg Arguments
                    startInfo.Arguments = "/C ffmpeg.exe -i no_audio.mkv -i Audio\\audio.mkv -c copy " + '\u0022' + outputfilename + '\u0022';
                    //Console.WriteLine(startInfo.Arguments);
                    process.StartInfo = startInfo;
                    process.Start();
                    process.WaitForExit();
                }

                pLabel.Dispatcher.Invoke(() => pLabel.Content = "Muxing completed!", DispatcherPriority.Background);
            }
        }

        //Kill all aomenc instances
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            Cancel.CancelAll = true;
            KillInstances();
            pLabel.Dispatcher.Invoke(() => pLabel.Content = "Cancled!", DispatcherPriority.Background);
        }

        public void KillInstances()
        {
            try
            {
                foreach (var process in Process.GetProcessesByName("aomenc"))
                {
                    process.Kill();
                }
                foreach (var process in Process.GetProcessesByName("ffmpeg"))
                {
                    process.Kill();
                }
            }
            catch { }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            try
            {
                //Delete Files, because of lazy dump****
                File.Delete("encoded.txt");
                Directory.Delete("Chunks", true);
                Directory.Delete("Audio", true);
                File.Delete("no_audio.mkv");
            }
            catch { }
        }

        //Some smaller Blackmagic, so parallel Workers won't lockdown the encoded.txt file
        private static ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();

        public void WriteToFileThreadSafe(string text, string path)
        {
            // Set Status to Locked
            _readWriteLock.EnterWriteLock();
            try
            {
                // Append text to the file
                using (StreamWriter sw = File.AppendText(path))
                {
                    sw.WriteLine(text);
                    sw.Close();
                }
            }
            finally
            {
                // Release lock
                _readWriteLock.ExitWriteLock();
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            //Disables editing of preset settings if user wants to write custom settings
            TextBoxCustomSettings.IsEnabled = true;
            ComboBoxEncMode.IsEnabled = false;
            TextBoxcqLevel.IsEnabled = false;
            TextBoxEncThreads.IsEnabled = false;
            ComboBoxBitdepth.IsEnabled = false;
            ComboBoxCpuUsed.IsEnabled = false;
            TextBoxTileCols.IsEnabled = false;
            TextBoxTileRows.IsEnabled = false;
            TextBoxKeyframeInterval.IsEnabled = false;
            TextBoxFramerate.IsEnabled = false;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            //Re-enables editing of preset settings
            TextBoxCustomSettings.IsEnabled = false;
            ComboBoxEncMode.IsEnabled = true;
            TextBoxcqLevel.IsEnabled = true;
            TextBoxEncThreads.IsEnabled = true;
            ComboBoxBitdepth.IsEnabled = true;
            ComboBoxCpuUsed.IsEnabled = true;
            TextBoxTileCols.IsEnabled = true;
            TextBoxTileRows.IsEnabled = true;
            TextBoxKeyframeInterval.IsEnabled = true;
            TextBoxFramerate.IsEnabled = true;
        }

        public void encodeAudio(string videoInput)
        {
            if (CheckBoxLogging.IsChecked == true)
            {
                WriteToFileThreadSafe(DateTime.Now.ToString("h:mm:ss tt") + " Audio Encoding started", "log.log");
            }
            string audioBitrate = "";

            audioBitrate = TextBoxAudioBitrate.Text;

            string allAudioSettings = "";
            //Sets Settings for Audio Encoding
            if (ComboBoxAudioCodec.Text == "Copy Audio")
            {
                if (CheckBoxLogging.IsChecked == true)
                {
                    WriteToFileThreadSafe(DateTime.Now.ToString("h:mm:ss tt") + " Audio Encoding Setting Encode Mode to Audio Copy", "log.log");
                }
                allAudioSettings = " -c:a copy";
            }
            else if (ComboBoxAudioCodec.Text == "Opus")
            {
                if (CheckBoxLogging.IsChecked == true)
                {
                    WriteToFileThreadSafe(DateTime.Now.ToString("h:mm:ss tt") + " Audio Encoding Setting Encode Mode to libopus", "log.log");
                }
                allAudioSettings = " -c:a libopus -b:a " + audioBitrate + "k ";
            }
            else if (ComboBoxAudioCodec.Text == "Opus 5.1")
            {
                if (CheckBoxLogging.IsChecked == true)
                {
                    WriteToFileThreadSafe(DateTime.Now.ToString("h:mm:ss tt") + " Audio Encoding Setting Encode Mode to libopus 5.1", "log.log");
                }
                allAudioSettings = " -c:a libopus -b:a " + audioBitrate + "k -af channelmap=channel_layout=5.1";
            }
            else if (ComboBoxAudioCodec.Text == "AAC CBR")
            {
                if (CheckBoxLogging.IsChecked == true)
                {
                    WriteToFileThreadSafe(DateTime.Now.ToString("h:mm:ss tt") + " Audio Encoding Setting Encode Mode to AAC CBR", "log.log");
                }
                allAudioSettings = " -c:a aac -b:a " + audioBitrate + "k ";
            }
            else if (ComboBoxAudioCodec.Text == "AC3")
            {
                if (CheckBoxLogging.IsChecked == true)
                {
                    WriteToFileThreadSafe(DateTime.Now.ToString("h:mm:ss tt") + " Audio Encoding Setting Encode Mode to AC3 CBR", "log.log");
                }
                allAudioSettings = " -c:a ac3 -b:a " + audioBitrate + "k ";
            }
            else if (ComboBoxAudioCodec.Text == "FLAC")
            {
                if (CheckBoxLogging.IsChecked == true)
                {
                    WriteToFileThreadSafe(DateTime.Now.ToString("h:mm:ss tt") + " Audio Encoding Setting Encode Mode to FLAC", "log.log");
                }
                allAudioSettings = " -c:a flac ";
            }
            else if (ComboBoxAudioCodec.Text == "MP3 CBR")
            {
                if (CheckBoxLogging.IsChecked == true)
                {
                    WriteToFileThreadSafe(DateTime.Now.ToString("h:mm:ss tt") + " Audio Encoding Setting Encode Mode to MP3 CBR", "log.log");
                }
                allAudioSettings = " -c:a libmp3lame -b:a " + audioBitrate + "k ";
            }
            else if (ComboBoxAudioCodec.Text == "MP3 VBR")
            {
                if (CheckBoxLogging.IsChecked == true)
                {
                    WriteToFileThreadSafe(DateTime.Now.ToString("h:mm:ss tt") + " Audio Encoding Setting Encode Mode to MP3 CBR", "log.log");
                }
                if (Int16.Parse(TextBoxAudioBitrate.Text) >= 10)
                {
                    MessageBox.Show("Audio VBR Range is from 0-9");
                }
                else if (Int16.Parse(TextBoxAudioBitrate.Text) <= 10)
                {
                    allAudioSettings = " -c:a libmp3lame -q:a " + audioBitrate + " ";
                }
            }

            //Sets the working directory
            string currentPath = Directory.GetCurrentDirectory();
            //Creates Audio Folder
            if (!Directory.Exists(Path.Combine(currentPath, "Audio")))
                Directory.CreateDirectory(Path.Combine(currentPath, "Audio"));

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";

            //FFmpeg Arguments
            if (CheckBoxLogging.IsChecked == true)
            {
                WriteToFileThreadSafe(DateTime.Now.ToString("h:mm:ss tt") + " Started Audio Encoding", "log.log");
            }
            startInfo.Arguments = "/C ffmpeg.exe -i " + '\u0022' + videoInput + '\u0022' + allAudioSettings + " -vn " + '\u0022' + "Audio\\audio.mkv" + '\u0022';
            if (CheckBoxLogging.IsChecked == true)
            {
                WriteToFileThreadSafe(DateTime.Now.ToString("h:mm:ss tt") + " Audio Encoding Ended", "log.log");
            }
            //Console.WriteLine(startInfo.Arguments);
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
        }

        public void getStreamFps(string fileinput)
        {
            string input = "";

            input = '\u0022' + fileinput + '\u0022';

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo = new System.Diagnostics.ProcessStartInfo()
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                Arguments = "/C ffprobe.exe -i " + input + " -v 0 -of csv=p=0 -select_streams v:0 -show_entries stream=r_frame_rate",
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            process.Start();
            // Now read the value, parse to int and add 1 (from the original script)
            //int online = int.Parse(process.StandardOutput.ReadToEnd()) + 1;
            string fpsOutput = process.StandardOutput.ReadLine();
            //string fpsOutputLine = new StringReader(fpsOutput).ReadLine();
            TextBoxFramerate.Text = fpsOutput;
            //Console.WriteLine(fpsOutput);
            process.WaitForExit();
        }
    }
}