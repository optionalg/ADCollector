﻿using System;
using System.DirectoryServices.Protocols;
using SearchOption = System.DirectoryServices.Protocols.SearchOption;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Security.Principal;
using System.Linq;
using System.DirectoryServices;

namespace ADCollector2
{
    internal static class Outputs
    {
        public static readonly Dictionary<string, string> gpos = new Dictionary<string, string>();

        public static void PrintSingle(SearchResponse response, string attr)
        {
            foreach (SearchResultEntry entry in response.Entries)
            {
                if (entry.Attributes[attr][0] is string)
                {
                    Console.WriteLine("  * {0}", entry.Attributes[attr][0]);
                }
                else if (entry.Attributes[attr][0] is byte[])
                {
                    Console.WriteLine("  * {0}",
                        System.Text.Encoding.ASCII.GetString((byte[])entry.Attributes[attr][0]));
                }
                else
                {
                    Console.WriteLine("Unexpected single-valued type: {0}", entry.Attributes[attr][0].GetType().Name);
                }
            }
        }


        //Only print the first attribute in the attrReturned array list

        public static void PrintMulti(SearchResponse response, string attr)
        {
            foreach (SearchResultEntry entry in response.Entries)
            {
                //Only if the attribute is specified to be returned, then "Attributes" contains it

                Console.WriteLine("  * {0}", entry.Attributes["sAMAccountName"][0]);
                Console.WriteLine("    {0}\n", entry.DistinguishedName);

                //in case the attribute value is null
                try
                {
                    if (attr == "") { }

                    else if (entry.Attributes[attr][0] is string)
                    {
                        for (int i = 0; i < entry.Attributes[attr].Count; i++)
                        {
                            Console.WriteLine("    - {0}: {1}", attr.ToUpper(), entry.Attributes[attr][i]);
                        }
                    }
                    else if (entry.Attributes[attr][0] is byte[])
                    {
                        for (int i = 0; i < entry.Attributes[attr].Count; i++)
                        {
                            Console.WriteLine("    - {0}: {1}",
                                attr.ToUpper(),
                                System.Text.Encoding.ASCII.GetString((byte[])entry.Attributes[attr][i]));
                        }
                    }
                    else
                    {
                        Console.WriteLine("Unexpected multi-valued type {0}", entry.Attributes[attr][0].GetType().Name);
                    }
                }
                catch { }
                
                Console.WriteLine();
            }
        }

        public static void PrintAll(SearchResponse response)//, string[] attrsList)
        {
            foreach (SearchResultEntry entry in response.Entries)
            {
                var attrs = entry.Attributes;

                foreach (DirectoryAttribute attr in attrs.Values)
                {
                    if (entry.Attributes[attr.Name][0] is string)
                    {
                        Console.WriteLine("  * {0} : {1}", attr.Name.ToUpper(), entry.Attributes[attr.Name][0]);
                    }
                    else if (entry.Attributes[attr.Name][0] is byte[])
                    {
                        Console.WriteLine("  *  {0}: {1}",
                            attr.Name.ToUpper(),
                            System.Text.Encoding.ASCII.GetString((byte[])entry.Attributes[attr.Name][0]));
                    }
                    else
                    {
                        Console.WriteLine("Unexpected type {0}", entry.Attributes[attr.Name][0].GetType().Name);
                    }
                }

                Console.WriteLine();
            }

        }


        ////myNames: { "myName" : "msDS-Name"}
        //public static void PrintMyName(SearchResponse response, Dictionary<string, string> myNames)
        //{
        //    foreach (SearchResultEntry entry in response.Entries)
        //    {
        //        foreach (KeyValuePair<string, string> pair in myNames)
        //        {
        //            Console.WriteLine("  * {0} : {1}", pair.Key, entry.Attributes[pair.Value][0]);
        //        }

        //        Console.WriteLine();
        //    }
        //}



        //public static void PrintAttrName(SearchResponse response)
        //{
        //    foreach (SearchResultEntry entry in response.Entries)
        //    {
        //        var attrs = entry.Attributes;

        //        foreach (DirectoryAttribute attr in attrs.Values)
        //        {
        //            Console.WriteLine("  *  " + attr.Name);
        //        }
        //        Console.WriteLine();
        //    }
        //}



        public static void PrintSPNs(SearchResponse response, string spnName)
        {
            ADCollector.UACFlags passNotExp = ADCollector.UACFlags.DONT_EXPIRE_PASSWD;

            foreach (SearchResultEntry entry in response.Entries)
            {
                var SPNs = entry.Attributes["servicePrincipalName"];

                var spnCount = SPNs.Count;

                //User accounts with SPN set
                if (spnName == "null")
                {
                    var uac = Enum.Parse(typeof(ADCollector.UACFlags), entry.Attributes["userAccountControl"][0].ToString());

                    Console.Write("  * sAMAccountName:  {0}", entry.Attributes["sAMAccountName"][0]);

                    if (uac.ToString().Contains(passNotExp.ToString()))
                    {
                        Console.WriteLine("    [DontExpirePasswd]");
                    }
                    else
                    {
                        Console.WriteLine();
                    }


                    for (int i = 0; i < spnCount; i++)
                    {
                        Console.WriteLine("    - {0}", SPNs[i]);
                    }
                    Console.WriteLine();
                }
                //Normal SPN scanning
                else
                {
                    Console.WriteLine("  * {0}", entry.Attributes["sAMAccountName"][0]);
                    //Console.WriteLine("    {0}", entry.DistinguishedName);

                    ////Print SPNs
                    if (spnCount > 1)
                    {
                        for (int i = 0; i < spnCount; i++)
                        {
                            if (SPNs[i].ToString().Split('/')[0].ToLower().Contains(spnName))
                            {
                                Console.WriteLine("    - {0}", SPNs[i]);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("    - {0}", SPNs[0]);
                    }
                    Console.WriteLine();
                }



            }
        }


        public static void PrintGPO(SearchResponse response)
        {
            foreach (SearchResultEntry entry in response.Entries)
            {
                string dn = entry.Attributes["cn"][0].ToString();
                string displayname = entry.Attributes["displayName"][0].ToString();

                //Console.WriteLine("  * CN : {0}", dn);
                //Console.WriteLine("  * DisplayName : {0}", displayname);

                gpos.Add(dn, displayname);

                //Console.WriteLine();
            }

        }



        public static void PrintDomainAttrs(SearchResponse response)
        {
            foreach (SearchResultEntry entry in response.Entries)
            {
                var pwdAge = TimeSpan.FromTicks(long.Parse(entry.Attributes["maxPWDAge"][0].ToString())).Days * -1;
                var lockduration = TimeSpan.FromTicks(long.Parse(entry.Attributes["LockoutDuration"][0].ToString())).Minutes * -1;
                Console.WriteLine("    MachineAccountQuota: {0}", entry.Attributes["ms-DS-MachineAccountQuota"][0]);
                Console.WriteLine("    MinPWDLength : {0}", entry.Attributes["minPWDLength"][0]);
                Console.WriteLine("    MaxPWDAge : {0} days", pwdAge);
                Console.WriteLine("    LockoutThreshold : {0}", entry.Attributes["lockoutThreshold"][0]);
                Console.WriteLine("    LockoutDuration : {0} Minutes", lockduration);
                Console.WriteLine("\n  * Group Policies linked to the domain object");
                Console.WriteLine();

                PrintGplink(entry);

            }
        }


        public static void PrintGplink(SearchResultEntry entry)
        {
            //non-greedy search
            Regex rx = new Regex(@"\{.+?\}", RegexOptions.Compiled);

            string gplinks = (string)entry.Attributes["gplink"][0];

            MatchCollection matches = rx.Matches(gplinks);

            foreach (Match match in matches)
            {
                Console.WriteLine("     - {0}", match.Value);
                Console.WriteLine("       {0}", gpos[match.Value]);
                Console.WriteLine();
            }
        }




        public static void PrintAce(ActiveDirectoryAccessRule rule, string forestDn)
        {
            //Adapted from https://github.com/PowerShellMafia/PowerSploit/blob/master/Recon/PowerView.ps1#L3746

            Regex rights = new Regex(@"(GenericAll)|(.*Write.*)|(.*Create.*)|(.*Delete.*)", RegexOptions.Compiled);
            Regex replica = new Regex(@"(.*Replication.*)", RegexOptions.Compiled);

            var sid = rule.IdentityReference.Translate(typeof(SecurityIdentifier)).ToString();

            if (int.Parse(sid.Split('-').Last()) > 1000)
            {
                if (rights.IsMatch(rule.ActiveDirectoryRights.ToString()) )
                {
                    Console.WriteLine("     IdentityReference:          {0}", rule.IdentityReference.ToString());
                    Console.WriteLine("     IdentitySID:                {0}", rule.IdentityReference.Translate(typeof(SecurityIdentifier)).ToString());
                    Console.WriteLine("     ActiveDirectoryRights:      {0}", rule.ActiveDirectoryRights.ToString());
                    Console.WriteLine();
                }
                else if ((rule.ActiveDirectoryRights.ToString() == "ExtendedRight" && rule.AccessControlType.ToString() == "Allow"))
                {
                    Console.WriteLine("     IdentityReference:          {0}", rule.IdentityReference.ToString());
                    Console.WriteLine("     IdentitySID:                {0}", rule.IdentityReference.Translate(typeof(SecurityIdentifier)).ToString());
                    Console.WriteLine("     ActiveDirectoryRights:      {0}", rule.ActiveDirectoryRights.ToString());

                    //The ObjectType GUID maps to an extended right registered in the current forest schema, then that specific extended right is granted
                    //Reference: https://www.blackhat.com/docs/us-17/wednesday/us-17-Robbins-An-ACE-Up-The-Sleeve-Designing-Active-Directory-DACL-Backdoors-wp.pdf

                    Console.WriteLine("     ObjectType:                 {0}", Functions.ResolveRightsGuids(forestDn, rule.ObjectType.ToString()));
                    Console.WriteLine();

                    
                }
            }

        }

    }
}
