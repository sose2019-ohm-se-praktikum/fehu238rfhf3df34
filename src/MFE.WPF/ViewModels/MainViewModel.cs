using Caliburn.Micro;
using GongSolutions.Wpf.DragDrop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace MFE.WPF.ViewModels
{
    public class MainViewModel : PropertyChangedBase, IDropTarget, IDisposable
    {
        public MainViewModel()
        {
            SoundFiles = new BindingList<SoundFileViewModel>() { RaiseListChangedEvents = true };
            IsRunning = false;
            BaseVolume = 1f;

            SoundFiles.ListChanged += SoundFilesListChanged;
        }
        
        ~MainViewModel()
        {
            Dispose();
        }

        public BindingList<SoundFileViewModel> SoundFiles { get; }

        private bool isRunning;
        public bool IsRunning
        {
            get
            {
                return isRunning;
            }
            set
            {
                isRunning = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(() => IsNotRunning);
                NotifyOfPropertyChange(() => CanStart);
            }
        }

        public bool IsNotRunning => !IsRunning;

        private int progress;
        public int Progress
        {
            get { return progress; }
            set
            {
                progress = value;
                NotifyOfPropertyChange();
            }
        }

        private float baseVolume;
        public float BaseVolume
        {
            get { return baseVolume; }
            set
            {
                baseVolume = value;
                NotifyOfPropertyChange();
            }
        }

        public bool CanStart
        {
            get
            {
                return !IsRunning && SoundFiles.Any(x => x.IsSelected);
            }
        }

        public async void Start()
        {
            foreach (var file in SoundFiles)
            {
                file.ThrownException = null;
                if (file.IsSelected)
                {
                    file.Status = Status.Pending;
                }
                else
                {
                    file.Status = Status.Skipped;
                }
            }

            IsRunning = true;
            var overmodulatedFiles = await AudioFileManager.CheckForOvermodulation(
                ProgressChanged,
                BaseVolume,
                SoundFiles.Where(x => x.IsSelected).Select(x => x.FullName),
                SoundFiles.Where(x => x.IsSelectedBaseVolumeFile).Select(x => x.FullName)
                );
            foreach (var file in overmodulatedFiles)
            {
                var soundFile = SoundFiles.First(x => x.FullName == file);
                if (soundFile != null)
                {
                    soundFile.Status = Status.Warning;
                    soundFile.IsSelected = false;
                }
            }

            await AudioFileManager.AdjustFiles(
                ProgressChanged,
                BaseVolume,
                SoundFiles.Where(x => x.IsSelected).Select(x => x.FullName),
                SoundFiles.Where(x => x.IsSelectedBaseVolumeFile).Select(x => x.FullName),
                FindFileOutputPath,
                FileSucceeded,
                FileFailed);
            IsRunning = false;

            foreach (var file in SoundFiles)
            {
                file.IsSelected = false;
                file.IsSelectedBaseVolumeFile = false;
            }
        }

        public void Remove(SoundFileViewModel soundFileViewModel)
        {
            if (soundFileViewModel != null)
            {
                AudioFileManager.CloseFile(soundFileViewModel.FullName);
                SoundFiles.Remove(soundFileViewModel);
            }
        }

        private void ProgressChanged(float progress)
        {
            Progress = (int)(progress * 100);
        }

        private string FindFileOutputPath(string file)
        {
            return SoundFiles.First(x => x.FullName == file).OutputFullName;
        }

        private void FileSucceeded(string file)
        {
            SoundFiles.First(x => x.FullName == file).Status = Status.Succeeded;
        }

        private void FileFailed(string file, Exception exception)
        {
            var soundFile = SoundFiles.First(x => x.FullName == file);
            soundFile.ThrownException = exception.ToString();
            soundFile.Status = Status.Failed;
        }

        private void SoundFilesListChanged(object sender, ListChangedEventArgs e)
        {
            NotifyOfPropertyChange(() => CanStart);
        }

        #region Drag & Drop
        public void DragOver(IDropInfo dropInfo)
        {
            var files = dropDataToFileList(dropInfo);
            dropInfo.Effects = IsRunning || files.Count == 0 ? DragDropEffects.None : DragDropEffects.Copy;
        }

        public void Drop(IDropInfo dropInfo)
        {
            var files = dropDataToFileList(dropInfo);
            if (!IsRunning && files.Count > 0)
            {
                var fileViewModels = files
                    .Select(file => Path.GetFullPath(file))
                    .Where(fullName => !SoundFiles.Any(x => x.FullName.Equals(fullName, StringComparison.OrdinalIgnoreCase)))
                    .Select(fullName => new SoundFileViewModel(fullName))
                    .ToList();
                foreach (var fileViewModel in fileViewModels)
                {
                    if (AudioFileManager.OpenFile(fileViewModel.FullName))
                    {
                        SoundFiles.Add(fileViewModel);
                    }
                }
            }

        }

        private List<string> dropDataToFileList(IDropInfo dropInfo)
        {
            return ((DataObject)dropInfo.Data).GetFileDropList().Cast<string>().ToList();
        }
        #endregion

        public void Dispose()
        {
            try
            {
                AudioFileManager.CloseAll();
            }
            catch (Exception)
            {
            }
        }
    }
}
