using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace PowerBaseWpf.Models
{
    public class User : PowerBase
    {

        #region Initialization
        public User()
        {
        }
        #endregion

        #region Secondary Objects

        private string _firstName;
        private string _username;
        private string _lastName;
        private string _email;
        private string _displayName;
        private string _fullName;
        private string _phone;
        private string _mobile;
        private string _fax;
        private string _otherPhone;
        private string _jobTitle;
        private string _userPrincipalName;
        private string _department;
        private string _description;
        private string _office;
        private string _userDomain;
        private string _computerDomain;
        private string _distinguishedName;
        private string _initials;
        private string _middleName;
        private string _computerName;

        #endregion

        #region Primary Objects

        public string UserDomain
        {
            get { return _userDomain; }
            set
            {
                if (value == _userDomain) return;
                _userDomain = value;
                NotifyPropertyChanged();
            }
        }

        public string ComputerDomain
        {
            get { return _computerDomain; }
            set
            {
                if (value == _computerDomain) return;
                _computerDomain = value;
                NotifyPropertyChanged();
            }
        }

        public string DistinguishedName
        {
            get { return _distinguishedName; }
            set
            {
                if (value == _distinguishedName) return;
                _distinguishedName = value;
                NotifyPropertyChanged();
            }
        }

        public string Username
        {
            get { return _username; }
            set
            {
                if (value == _username) return;
                _username = value;
                NotifyPropertyChanged();
            }
        }

        public string FirstName
        {
            get { return _firstName; }
            set
            {
                if (value == _firstName) return;
                _firstName = value;
                NotifyPropertyChanged();
            }
        }

        public string LastName
        {
            get { return _lastName; }
            set
            {
                if (value == _lastName) return;
                _lastName = value;
                NotifyPropertyChanged();
            }
        }

        public string Initials
        {
            get { return _initials; }
            set
            {
                if (value == _initials) return;
                _initials = value;
                NotifyPropertyChanged();
            }
        }

        public string MiddleName
        {
            get { return _middleName; }
            set
            {
                if (value == _middleName) return;
                _middleName = value;
                NotifyPropertyChanged();
            }
        }

        public string Email
        {
            get { return _email; }
            set
            {
                if (value == _email) return;
                _email = value;
                NotifyPropertyChanged();
            }
        }

        public string DisplayName
        {
            get { return _displayName; }
            set
            {
                if (value == _displayName) return;
                _displayName = value;
                NotifyPropertyChanged();
            }
        }

        public string FullName
        {
            get { return _fullName; }
            set
            {
                if (value == _fullName) return;
                _fullName = value;
                NotifyPropertyChanged();
            }
        }

        public string Phone
        {
            get { return _phone; }
            set
            {
                if (value == _phone) return;
                _phone = value;
                NotifyPropertyChanged();
            }
        }

        public string Mobile
        {
            get { return _mobile; }
            set
            {
                if (value == _mobile) return;
                _mobile = value;
                NotifyPropertyChanged();
            }
        }

        public string Fax
        {
            get { return _fax; }
            set
            {
                if (value == _fax) return;
                _fax = value;
                NotifyPropertyChanged();
            }
        }

        public string OtherPhone
        {
            get { return _otherPhone; }
            set
            {
                if (value == _otherPhone) return;
                _otherPhone = value;
                NotifyPropertyChanged();
            }
        }

        public string JobTitle
        {
            get { return _jobTitle; }
            set
            {
                if (value == _jobTitle) return;
                _jobTitle = value;
                NotifyPropertyChanged();
            }
        }

        public string UserPrincipalName
        {
            get { return _userPrincipalName; }
            set
            {
                if (value == _userPrincipalName) return;
                _userPrincipalName = value;
                NotifyPropertyChanged();
            }
        }

        public string Department
        {
            get { return _department; }
            set
            {
                if (value == _department) return;
                _department = value;
                NotifyPropertyChanged();
            }
        }

        public string Description
        {
            get { return _description; }
            set
            {
                if (value == _description) return;
                _description = value;
                NotifyPropertyChanged();
            }
        }

        public string Office

        {
            get { return _office; }
            set
            {
                if (value == _office) return;
                _office = value;
                NotifyPropertyChanged();
            }
        }

        public string ComputerName
        {
            get { return _computerName; }
            set
            {
                if (value == _computerName) return;
                _computerName = value;
                NotifyPropertyChanged();
            }
        }

        #endregion
    }
}
