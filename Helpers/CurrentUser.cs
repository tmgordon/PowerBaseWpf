using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.ActiveDirectory;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Windows;
using PowerBaseWpf.Models;

namespace PowerBaseWpf.Helpers
{
    public static class CurrentUser
    {
        public static bool Loaded { get; set; }
        public static User User { get; set; }
        public static bool IsLocal { get; set; }
        public static List<string> DomainControllerList { get; set; } = new List<string>();
        public static void Initialize()
        {
            if (IsDomainMember())
            {
                var user = UserPrincipal.Current;
                User = new User()
                {
                    FullName = user.Name,
                    FirstName = user.GivenName,
                    LastName = user.Surname,
                    MiddleName = user.MiddleName,
                    Username = user.SamAccountName,
                    UserPrincipalName = user.UserPrincipalName,
                    DisplayName = user.DisplayName,
                    Email = user.EmailAddress,
                    UserDomain = Domain.GetCurrentDomain().Name,
                    ComputerDomain = Domain.GetComputerDomain().Name,
                    ComputerName = Environment.MachineName,
                };
                
                foreach (var dc in Domain.GetComputerDomain().DomainControllers)
                {
                    DomainControllerList.Add(dc.ToString());
                }

            }else{
                IsLocal = true;
                User = new User()
                {
                    Username = Environment.UserName,
                    UserDomain = Environment.UserDomainName,
                    ComputerName = Environment.MachineName,
                    ComputerDomain = Domain.GetComputerDomain().Name,
                };
            }
            Loaded = true;
        }


        public static bool IsDomainMember()
        {
            ManagementObject ComputerSystem;
            using (ComputerSystem = new ManagementObject($"Win32_ComputerSystem.Name='{Environment.MachineName}'"))
            {
                ComputerSystem.Get();
                object Result = ComputerSystem["PartOfDomain"];
                return (Result != null && (bool) Result);
            }
        }

    }
}
