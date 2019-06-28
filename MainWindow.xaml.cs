using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace InstallerApplication
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<string> lstStrInstalledComponent = new List<string>();
        Dictionary<string, List<string>> dictDependencyComponents = new Dictionary<string, List<string>>();

        public MainWindow()
        {
            InitializeComponent();
            //dictDependencyComponents.Add("FTP", new List<string>(new string[] { "TCPIP" }));
            //dictDependencyComponents.Add("TELNET", new List<string>(new string[] { "TCPIP", "NETCARD" }));
            //dictDependencyComponents.Add("TCPIP", new List<string>(new string[] { "NETCARD" }));
            //dictDependencyComponents.Add("DNS", new List<string>(new string[] { "TCPIP", "NETCARD" }));
            //dictDependencyComponents.Add("BROWSER", new List<string>(new string[] { "TCPIP", "HTML" }));
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            lstStrInstalledComponent.Clear();
            lstBoxInstallOutput.Items.Clear();
            dictDependencyComponents.Clear();
            int intIdxLineCount = 0;
            int intIteration = 0;

            //first create a dictionary of depend components based on the input script
            while (intIdxLineCount < txtBox.LineCount)
            {
                string strCommandLine = txtBox.GetLineText(intIdxLineCount).Replace("\r\n", " ").Trim();
                string[] strArrCommandLineItems = System.Text.RegularExpressions.Regex.Split(strCommandLine, @"\s+");
                int intIdxChildElemInDepend = 0;
                if (strArrCommandLineItems[0] == "DEPEND")
                {
                    lstBoxInstallOutput.Items.Add(strCommandLine);

                    string strKey = string.Empty;
                    intIdxChildElemInDepend = 0;
                    List<string> lstStrValues = new List<string>();
                    foreach (string strEachItemCmdLine in strArrCommandLineItems)
                    {
                        if (strEachItemCmdLine != "DEPEND")
                        {
                            if (intIdxChildElemInDepend == 0)
                            {
                                strKey = strEachItemCmdLine;
                                intIdxChildElemInDepend = 1;
                            }
                            else
                            {
                                lstStrValues.Add(strEachItemCmdLine);
                            }
                        }
                    }
                    dictDependencyComponents.Add(strKey, lstStrValues);
                }
                intIdxLineCount++;
            }

            if (dictDependencyComponents.Keys.Count == 0)
            {
                MessageBox.Show("No Dependency components found\nPLease write atleast one dependency to continue executing the script", "Dependency Missing", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                intIdxLineCount = 0;
                while (intIdxLineCount < txtBox.LineCount)
                {
                    string strCommandLine = txtBox.GetLineText(intIdxLineCount).Replace("\r\n", " ").Trim();
                    string[] strArrCommandLine = System.Text.RegularExpressions.Regex.Split(strCommandLine, @"\s+");

                    if (strArrCommandLine[0] == "INSTALL")
                    {
                        //check if there is only one install component
                        if (strArrCommandLine.Length == 2)
                        {
                            lstBoxInstallOutput.Items.Add(strCommandLine);
                            InstallComponent(strArrCommandLine[1].ToString(), intIteration);
                        }
                    }
                    else if (strArrCommandLine[0] == "LIST")
                    {
                        //check if there is only one list command
                        if (strArrCommandLine.Length == 1)
                        {
                            lstBoxInstallOutput.Items.Add(strCommandLine);
                            foreach (string strInstalledComp in lstStrInstalledComponent)
                            {
                                lstBoxInstallOutput.Items.Add(" " + strInstalledComp);
                            }
                        }
                    }
                    else if (strArrCommandLine[0] == "REMOVE")
                    {
                        //check if there is only one install component
                        if (strArrCommandLine.Length == 2)
                        {
                            lstBoxInstallOutput.Items.Add(strCommandLine);
                            RemoveComponent(strArrCommandLine[1].ToString());
                        }
                    }

                    else if (strArrCommandLine[0] == "END")
                    {
                        //Print END at the end of the listbox
                        lstBoxInstallOutput.Items.Add(strCommandLine);
                    }
                    intIdxLineCount++;
                }
            }
        }

        private void RemoveComponent(string ipStrRemoveComponent)
        {
            if (!lstStrInstalledComponent.Contains(ipStrRemoveComponent))
            {
                lstBoxInstallOutput.Items.Add(" " + ipStrRemoveComponent + " is not installed");
            }
            else if (lstStrInstalledComponent.Contains(ipStrRemoveComponent))
            {
                //if remove component is a key of dependency then checking
                //all the parent components for their child dependency to remove
                if (dictDependencyComponents.ContainsKey(ipStrRemoveComponent))
                {
                    //try removing child element which is at key currently first 
                    //by checking if it is parent of any of the installed components and 
                    //if it can be deleted then only check for its child componends
                    int intIdxKeyElementDeleted = 0;
                    foreach (KeyValuePair<string, List<string>> detailData in dictDependencyComponents)
                    {
                        if (detailData.Key != ipStrRemoveComponent)
                        {
                            if (intIdxKeyElementDeleted == 0)
                            {
                                foreach (string strEachKeyValue in detailData.Value)
                                {
                                    //if a parent name is found as a value at specific key element as a dependent 
                                    //then check if the dependent is installed or not
                                    if (strEachKeyValue == ipStrRemoveComponent)
                                    {
                                        if (lstStrInstalledComponent.Contains(detailData.Key))
                                        {
                                            //lstBoxInstallOutput.Items.Add(" " + ipStrRemoveComponent + " is still needed");
                                            intIdxKeyElementDeleted = 1;
                                            break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    if (intIdxKeyElementDeleted == 0)
                    {
                        //remove the child element from the installed list and also from the output result
                        lstBoxInstallOutput.Items.Add(" Removing " + ipStrRemoveComponent);
                        lstStrInstalledComponent.Remove(ipStrRemoveComponent);

                        //now proceed removing the parent elements checking its child dependecy as now the main child element is removed 
                        //so parents are supposed to be checked if they are need by other components or not
                        List<string> lstStrParentComponents = new List<string>();
                        dictDependencyComponents.TryGetValue(ipStrRemoveComponent, out lstStrParentComponents);
                        int intRequiredParentComponent;
                        lstStrParentComponents.Reverse();
                        foreach (string strEachParentComponent in lstStrParentComponents)
                        {
                            intRequiredParentComponent = 0;
                            CheckForRequiredParentComponent(ipStrRemoveComponent, strEachParentComponent, ref intRequiredParentComponent);
                            if (intRequiredParentComponent == 0)
                            {
                                lstBoxInstallOutput.Items.Add(" Removing " + strEachParentComponent);
                                lstStrInstalledComponent.Remove(strEachParentComponent);
                            }
                        }
                    }
                    else if (intIdxKeyElementDeleted == 1)
                    {
                        lstBoxInstallOutput.Items.Add(" " + ipStrRemoveComponent + " is still needed.");
                    }
                }

                else if (!dictDependencyComponents.ContainsKey(ipStrRemoveComponent))
                {
                    int intRequiredParentComponent;
                    intRequiredParentComponent = 0;
                    foreach (KeyValuePair<string, List<string>> detailData in dictDependencyComponents)
                    {
                        if (intRequiredParentComponent == 0)
                        {
                            foreach (string strEachKeyValue in detailData.Value)
                            {
                                //if a parent name is found as a value at specific key element as a dependent 
                                //then check if the dependent is installed or not
                                if (strEachKeyValue == ipStrRemoveComponent)
                                {
                                    if (lstStrInstalledComponent.Contains(detailData.Key))
                                    {
                                        //lstBoxInstallOutput.Items.Add(" " + ipStrRemoveComponent + " is still needed");
                                        intRequiredParentComponent = 1;
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (intRequiredParentComponent == 0)
                    {
                        lstBoxInstallOutput.Items.Add(" Removing " + ipStrRemoveComponent);
                        lstStrInstalledComponent.Remove(ipStrRemoveComponent);
                    }
                    else if (intRequiredParentComponent == 1)
                    {
                        lstBoxInstallOutput.Items.Add(" " + ipStrRemoveComponent + " is still needed.");
                    }
                }
            }
        }

        private void CheckForRequiredParentComponent(string ipStrMainRemoveComponent, string ipStrParentComponent, ref int intRequiredParentComponent)
        {
            foreach (KeyValuePair<string, List<string>> detailData in dictDependencyComponents)
            {
                if (detailData.Key != ipStrParentComponent && detailData.Key != ipStrMainRemoveComponent)
                {
                    if (intRequiredParentComponent == 0)
                    {
                        foreach (string strEachKeyValue in detailData.Value)
                        {
                            //if a parent name is found as a value at specific key element as a dependent 
                            //then check if the dependent is installed or not
                            if (strEachKeyValue == ipStrParentComponent)
                            {
                                if (lstStrInstalledComponent.Contains(detailData.Key))
                                {
                                    //lstBoxInstallOutput.Items.Add(" " + ipStrRemoveComponent + " is still needed");
                                    intRequiredParentComponent = 1;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private void InstallComponent(string ipStrInstallComponent, int intIteration)
        {
            if (lstStrInstalledComponent.Contains(ipStrInstallComponent) && intIteration == 0)
            {
                lstBoxInstallOutput.Items.Add(" " + ipStrInstallComponent + " is already installed.");
            }
            else if (!lstStrInstalledComponent.Contains(ipStrInstallComponent))
            {
                if (!dictDependencyComponents.ContainsKey(ipStrInstallComponent))
                {
                    lstBoxInstallOutput.Items.Add(" Installing " + ipStrInstallComponent);
                    lstStrInstalledComponent.Add(ipStrInstallComponent);
                }
                else
                {
                    List<string> lstStrParentComponents = new List<string>();
                    dictDependencyComponents.TryGetValue(ipStrInstallComponent, out lstStrParentComponents);
                    foreach (string strEachParentComponent in lstStrParentComponents)
                    {
                        InstallComponent(strEachParentComponent, 1);
                    }
                    lstBoxInstallOutput.Items.Add(" Installing " + ipStrInstallComponent);
                    lstStrInstalledComponent.Add(ipStrInstallComponent);
                }
            }
        }
    }
}
