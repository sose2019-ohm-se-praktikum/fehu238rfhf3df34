using Caliburn.Micro;
using System;

namespace MFE.WPF.ViewModels
{
    public enum Status
    {
        Skipped,
        Pending,
        Succeeded,
        Failed
    }

    public class SoundFileViewModel : PropertyChangedBase
    {
        public SoundFileViewModel(string fullName)
        {
            this.fullName = fullName;
            this.outputFullName = fullName;
        }

        private string fullName;
        public string FullName
        {
            get { return fullName; }
            set
            {
                fullName = value;
                NotifyOfPropertyChange();

                if (OutputFullName == null || OutputFullName.Length == 0)
                {
                    OutputFullName = value;
                }
            }
        }

        private string outputFullName;
        public string OutputFullName
        {
            get { return outputFullName; }
            set
            {
                outputFullName = value;
                NotifyOfPropertyChange();
            }
        }

        private bool isSelected;
        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                if (isSelected != value)
                {
                    isSelected = value;
                    NotifyOfPropertyChange();

                    if (value)
                    {
                        // selected
                        AudioFileManager.OpenFile(FullName);
                    }
                    else
                    {
                        // deselected
                        AudioFileManager.CloseFile(FullName);
                    }
                }
            }
        }

        private bool isSelectedBaseVolumeFile;
        public bool IsSelectedBaseVolumeFile
        {
            get { return isSelectedBaseVolumeFile; }
            set
            {
                isSelectedBaseVolumeFile = value;
                NotifyOfPropertyChange();
            }
        }

        private Status status;
        public Status Status
        {
            get { return status; }
            set
            {
                if (status != value)
                {
                    status = value;
                    NotifyOfPropertyChange();
                    NotifyOfPropertyChange(() => ShowTooltip);
                }
            }
        }

        private string thrownException;
        public String ThrownException
        {
            get { return thrownException; }
            set
            {
                if (thrownException != value)
                {
                    thrownException = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public bool ShowTooltip
        {
            get
            {
                return Status == Status.Failed;
            }
        }
    }
}
