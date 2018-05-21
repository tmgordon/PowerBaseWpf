using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading.Tasks;
using System.DirectoryServices.ActiveDirectory;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Management.Automation;
using System.Text.RegularExpressions;
using PowerBaseWpf.Models;
using ActiveDs;

namespace PowerBaseWpf.Helpers
{
    public static class AdConvert
    {
        public static Dictionary<string, int> TransCodes = new Dictionary<string, int>()
        {
            { "DN", (int)ADS_NAME_TYPE_ENUM.ADS_NAME_TYPE_1779 },
            { "CN", (int)ADS_NAME_TYPE_ENUM.ADS_NAME_TYPE_CANONICAL },
            { "UPN", (int)ADS_NAME_TYPE_ENUM.ADS_NAME_TYPE_DOMAIN_SIMPLE },
            { "NT4", (int) ADS_NAME_TYPE_ENUM.ADS_NAME_TYPE_NT4 },
            { "GUID", (int) ADS_NAME_TYPE_ENUM.ADS_NAME_TYPE_GUID },
            { "DISPLAY", (int) ADS_NAME_TYPE_ENUM.ADS_NAME_TYPE_DISPLAY },
            { "?", (int) ADS_NAME_TYPE_ENUM.ADS_NAME_TYPE_UNKNOWN },
            
        };



        public static string VerifyCanonicalName(string cn)
        {
            string output = "";
            if (cn != null)
            {
                output = (cn.Contains("CN=") && cn.Contains("DC=")) ? DistinguishedNameToCanonicalName(cn) : cn;
            }
            
            if (output.Contains("/"))
            {
                return output;
            }

            return "";

        }

        public static string VerifyDistinguishedName(string dn)
        {
            string output = "";

            if (dn != null)
            {
                output = (dn.Contains("/")) ? CanonicalNameToDistinguishedName(dn) : dn;
            }

            if (output.Contains("CN=") && output.Contains("DC="))
            {
                return output;
            }

            return "";
        }

        public static string CanonicalNameToDistinguishedName(string cn)
        {
            var adTranslate = new NameTranslate();
            adTranslate.Set((int) ADS_NAME_TYPE_ENUM.ADS_NAME_TYPE_CANONICAL, cn);
            return adTranslate.Get((int) ADS_NAME_TYPE_ENUM.ADS_NAME_TYPE_1779);
        }

        public static string DistinguishedNameToCanonicalName(string dn)
        {
            var adTranslate = new NameTranslate();
            adTranslate.Set((int) ADS_NAME_TYPE_ENUM.ADS_NAME_TYPE_1779, dn);
            return adTranslate.Get((int) ADS_NAME_TYPE_ENUM.ADS_NAME_TYPE_CANONICAL);
        }

        public static string DistinguishedNameToDisplay(string dn)
        {
            var adTranslate = new NameTranslate();
            adTranslate.Set((int) ADS_NAME_TYPE_ENUM.ADS_NAME_TYPE_1779, dn);
            return adTranslate.Get((int) ADS_NAME_TYPE_ENUM.ADS_NAME_TYPE_DISPLAY);
        }

        public static string DistinguishedNameToUpn(string dn)
        {
            var adTranslate = new NameTranslate();
            adTranslate.Set((int) ADS_NAME_TYPE_ENUM.ADS_NAME_TYPE_1779, dn);
            return adTranslate.Get((int) ADS_NAME_TYPE_ENUM.ADS_NAME_TYPE_DOMAIN_SIMPLE);
        }

        public static string DistinguishedNameToEmail(string dn)
        {
            var ldap = $"LDAP://{dn}";
            DirectoryEntry adObj = new DirectoryEntry(ldap);
            var mail = adObj.Properties["mail"].Value?.ToString() ?? "";
            return mail;
        }

        public static string GetDistinguishedName(string user, string domain)
        {
            if (user.Contains("DC="))
            {
                return user;
            }

            if (user.Contains("@"))
            {
                // Use EmailtoDN (email or sAMAccount)
                return EmailToDistinguishedName(user, domain);
            }

            if (user.Contains("/"))
            {
                // Use CN / AD
                return CanonicalNameToDistinguishedName(user);
            }

            return user;
        }

        public static User GetUserByIdentity(string identity, string domain)
        {
            var domainDN = DomainToDistinguishedName(domain);
            var context = new PrincipalContext(ContextType.Domain, domain);
            if (identity.Contains("CN="))
            {
                var up = UserPrincipal.FindByIdentity(context, IdentityType.DistinguishedName, identity);
                if (up != null)
                {
                    var user = new User()
                    {
                        FirstName = up.GivenName,
                        LastName = up.Surname,
                        FullName = up.Name,
                        Username = up.SamAccountName,
                        Email = up.EmailAddress,
                        DisplayName = up.DisplayName,
                        DistinguishedName = up.DistinguishedName,
                    };
                    return user;
                }
            }

            if (identity.Contains("@"))
            {
                var adEntry = new DirectoryEntry("LDAP://" + domainDN);
                DirectorySearcher adSearcher = new DirectorySearcher(adEntry);
                adSearcher.Filter = ("mail=" + identity);
                SearchResultCollection results = adSearcher.FindAll();
                DirectoryEntry adObj;
                if (results.Count == 1)
                {
                    adObj = results[0].GetDirectoryEntry();
                    var user = new User
                    {
                        FirstName = adObj.Properties["givenName"].Value?.ToString(),
                        LastName = adObj.Properties["sn"].Value?.ToString(),
                        DisplayName = adObj.Properties["displayName"].Value?.ToString(),
                        FullName = adObj.Properties["name"].Value?.ToString(),
                        Username = adObj.Properties["sAMAccountName"].Value?.ToString(),
                        Email = adObj.Properties["mail"].Value?.ToString(),
                        DistinguishedName = adObj?.Properties["distinguishedName"].Value?.ToString()
                    };
                    return user;
                }

                var sp = identity.Split('@');
                var sam = sp[0];
                var up = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, sam);
                if (up != null)
                {
                    var user = new User()
                    {
                        FirstName = up.GivenName,
                        LastName = up.Surname,
                        DisplayName = up.DisplayName,
                        FullName = up.Name,
                        Username = up.SamAccountName,
                        Email = up.EmailAddress,
                        DistinguishedName = up.DistinguishedName,
                    };
                    return user;
                }
                up = UserPrincipal.FindByIdentity(context, IdentityType.UserPrincipalName, identity);
                if (up != null)
                {
                    var user = new User()
                    {
                        FirstName = up.GivenName,
                        LastName = up.Surname,
                        FullName = up.Name,
                        Username = up.SamAccountName,
                        Email = up.EmailAddress,
                        DisplayName = up.DisplayName,
                        DistinguishedName = up.DistinguishedName,
                    };
                    return user;
                }
            }
            return new User();
        }
        
        public static string EmailToDistinguishedName(string email, string domain)
        {
            var domainDN = DomainToDistinguishedName(domain);
            var adEntry = new DirectoryEntry("LDAP://" + domainDN);
            DirectorySearcher adSearcher = new DirectorySearcher(adEntry);
            adSearcher.Filter = ("mail=" + email);
            SearchResultCollection results = adSearcher.FindAll();
            DirectoryEntry adObj;
            if (results.Count < 1)
            {
                adSearcher.Filter = ("sAMAccountName=" + email);
                results = adSearcher.FindAll();
                if (results.Count < 1) return null;
                adObj = results[0].GetDirectoryEntry();
                var dn = adObj.Properties["distinguishedName"].Value?.ToString() ?? "";
                return dn;
            }
            else
            {
                adObj = results[0].GetDirectoryEntry();
                var dn = adObj.Properties["distinguishedName"].Value?.ToString() ?? "";
                return dn;
            }
        }

        public static string DomainToDistinguishedName(string domain)
        {
            try
            {
                DirectoryContext context = new DirectoryContext(DirectoryContextType.Domain, domain);
                Domain d = Domain.GetDomain(context);
                DirectoryEntry de = d.GetDirectoryEntry();
                return de.Properties["DistinguishedName"].Value.ToString();
            }
            catch (Exception err)
            {
                var DomainSplit = domain.Split('.');
                var dn = "";
                if (DomainSplit.Length == 3)
                {
                    dn = $"DC={DomainSplit[0]},DC={DomainSplit[1]},DC={DomainSplit[2]}";
                }

                if (DomainSplit.Length == 2)
                {
                    dn = $"DC={DomainSplit[0]},DC={DomainSplit[1]}";
                }

                if (DomainSplit.Length < 2)
                {
                    dn = $"CN={domain}";
                }
                return dn;
            }
        }


    }

    public enum ADS_NAME_TYPE_ENUM
    {
        ADS_NAME_TYPE_1779 = 1,
        ADS_NAME_TYPE_CANONICAL = 2,
        ADS_NAME_TYPE_NT4 = 3,
        ADS_NAME_TYPE_DISPLAY = 4,
        ADS_NAME_TYPE_DOMAIN_SIMPLE = 5,
        ADS_NAME_TYPE_ENTERPRISE_SIMPLE = 6,
        ADS_NAME_TYPE_GUID = 7,
        ADS_NAME_TYPE_UNKNOWN = 8,
        ADS_NAME_TYPE_USER_PRINCIPAL_NAME = 9,
        ADS_NAME_TYPE_CANONICAL_EX = 10,
        ADS_NAME_TYPE_SERVICE_PRINCIPAL_NAME = 11,
        ADS_NAME_TYPE_SID_OR_SID_HISTORY_NAME = 12,
    }

    public class DeconstructedDn
    {
        public string OrganizationalUnit { get; set; }
        public string BaseCn { get; set; }
        public string DomainName { get; set; }
        public string FullCn { get; set; }
        public string DistinguishedName { get; set; }
        public string OrganizationalUnitDn { get; set; }
        public string OrganizationalUnitCn { get; set; }
        public DeconstructedDn()
        {

        }
        public DeconstructedDn(string dn)
        {
            Parse(dn);
        }

        public void Parse(string dn)
        {
            DistinguishedName = dn;
            BaseCn = "";
            var ouList = new List<string>();
            var str = dn;
            var cnRegex = Regex.Match(str, $@"(CN=)(.*?)(,)(OU|DC)(=)");
            if (cnRegex.Success)
            {
                BaseCn = cnRegex.Groups[2].Value;
                var cnr = $"{cnRegex.Groups[1].Value}{cnRegex.Groups[2].Value}{cnRegex.Groups[3].Value}";
                var sp = str.Split(new[] { cnr }, StringSplitOptions.None);
                if (sp.Length > 0)
                {
                    str = sp[1];
                }
            }
            OuPartialDn = str;

            while (Regex.Match(str, $@"(OU)(=)(.*?)(,)(OU|DC)(=)").Success)
            {
                var ouRegex = Regex.Match(str, $@"(OU)(=)(.*?)(,)(OU|DC)(=)");
                ouList.Add(ouRegex.Groups[3].Value);
                var match = $"{ouRegex.Groups[1].Value}{ouRegex.Groups[2].Value}{ouRegex.Groups[3].Value}{ouRegex.Groups[4].Value}";
                var sp = str.Split(new[] { match }, StringSplitOptions.None);
                str = sp[1];
            }

            if (str.IndexOf("DC", StringComparison.InvariantCulture) != 0) return;

            str = str.Replace("DC=", "");
            str = str.Replace(",", ".");
            DomainName = str;
            var orderedOu = new string[ouList.Count];
            var x = ouList.Count;
            foreach (var ou in ouList)
            {
                x = x - 1;
                orderedOu[x] = ou;
            }
            JoinedOu = string.Join("/", orderedOu);
            OrganizationalUnit = ouList[0];
            OrganizationalUnitDn = $"{OuPartialDn}";
            FullCn = $"{DomainName}/{JoinedOu}/{BaseCn}";
            OrganizationalUnitCn = $"{DomainName}/{JoinedOu}";
            return;
        }

        private string JoinedOu { get; set; }
        private string OuPartialDn { get; set; }
    }
}
