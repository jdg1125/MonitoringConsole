﻿using System.Text;
using static MonitoringConsole.Models.SessionData;

namespace MonitoringConsole.Library
{
    public class KeylogParsing
    {
        public static string ParseAWSMessage(string s)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("AWS Alert - possible WorkSpace attack. ");

            int index = s.IndexOf("IP Address: ") + 12;
            s = s.Substring(index, s.Length - index);
            sb.Append(s.Substring(0, s.IndexOf(' ')) + " ");

            index = s.IndexOf("WorkspaceId: ") + 13;
            s = s.Substring(index, s.Length - index);
            sb.Append(s.Substring(0, s.IndexOf(' ')) + " ");

            index = s.IndexOf("Username: ") + 10;
            s = s.Substring(index, s.Length - index);
            sb.Append(s.Substring(0, s.IndexOf(' ')));

            return sb.ToString();
        }


        public static string FindCommands(string s)
        {
            StringBuilder sb = new StringBuilder();
            int len = s.Length;

            for (int i = 0; i < len; i++)
            {
                if (s[i] == '[')
                {
                    int j;
                    for (j = i + 1; j < len; j++)
                    {
                        if (s[j] == '[')
                        {
                            sb.Append('['); // [  was possibly entered by user. Append and continue scan
                            break;
                        }
                        else if (s[j] == ']')
                        {
                            string sub;
                            if (IgnoredKeys.Contains(sub = s.Substring(i, j - i + 1)) == false && sub != "[ENTER]")
                                sb.Append(sub);
                            else if (sub == "[ENTER]")
                            {
                                string draft = sb.ToString();
                                int index;
                                if ((index = draft.IndexOf('\r')) >= 0 && index + 1 < draft.Length)
                                    draft = draft.Substring(index + 1);
                                if ((index = draft.IndexOf('\n')) >= 0 && index + 1 < draft.Length)
                                    draft = draft.Substring(index + 1);

                                if (draft != "" && draft != "\n")
                                    CommandsEntered.Add(draft);
                                sb.Clear();
                            }

                            i = j;
                            break;
                        }
                    }
                    if (j == len) //we got to the end of the message without seeing another [  or ]
                    {
                        sb.Append(s.Substring(i, j - i));
                        break;
                    }
                }
                else
                    sb.Append(s[i]);
            }

            return sb.ToString();
        }


        public static void ParseCmd(int index)
        {
            int ptr = index;
            string opString = CommandsEntered[ptr];

            string cmd = opString = ProcessUpDown(opString, ref ptr);

            if (cmd != CommandsEntered[ptr]) //an UP/DOWN token was substituted
            {
                cmd = CommandsEntered[ptr]; //need to use String.Copy(...) ? 
                cmd = ProcessBkspDel(cmd, opString); //opString contains the keystrokes following the last UP/DOWN token
            }
            else
            {
                int i = cmd.IndexOf('[');
                if (i >= 0)
                {
                    string sub = cmd.Substring(0, i);
                    cmd = ProcessBkspDel(sub, cmd.Substring(i, cmd.Length - i));
                }
            }

            CommandsEntered[index] = cmd;
        }


        private static string ProcessUpDown(string opString, ref int ptr)
        {
            int up = -1, down = -1;
            int index = ptr; //effectively past the bottom of the list - once an UP has been performed, the cmd here is no longer accessible

            while (true)
            {
                up = opString.IndexOf("[UP]");
                down = opString.IndexOf("[DOWN]");
                //Console.WriteLine(up + ", " + down);

                if (up >= 0 && down >= 0)
                {
                    if (up < down) //try to decrement ptr & skip the up token
                        HandleUp(ref opString, up, ref ptr);

                    else  //try to increment ptr & skip down token
                        HandleDown(ref opString, down, ref ptr, index);
                }

                else if (up >= 0)
                    HandleUp(ref opString, up, ref ptr);
                else if (down >= 0)
                    HandleDown(ref opString, down, ref ptr, index);
                else
                    break;
            }

            return opString; //the suffix of the string, containing the first non-up/down token char, OR ""
        }

        private static void HandleUp(ref string opString, int up, ref int ptr)
        {
            if (ptr > 0)
                ptr--;

            //Console.WriteLine("in handleup: " + ptr);

            if (up + 4 < opString.Length)
                opString = opString.Substring(up + 4, opString.Length - up - 4);
            else
                opString = "";
        }

        private static void HandleDown(ref string opString, int down, ref int ptr, int index)
        {
            if (ptr + 1 < index)
                ptr++;

            //Console.WriteLine("Here");

            if (down + 6 < opString.Length)
                opString = opString.Substring(down + 6, opString.Length - down - 6);
            else
                opString = "";
        }

        private static string ProcessBkspDel(string cmd, string opString)
        {
            StringBuilder sb = new StringBuilder(cmd);
            int cursor = cmd.Length;
            int len = opString.Length;


            for (int i = 0; i < len; i++)
            {
                if (opString[i] == '[')
                {
                    int j;
                    for (j = i + 1; j < len; j++)
                    {
                        if (opString[j] == '[')
                        {
                            sb.Append('[');
                            break;
                        }
                        else if (opString[j] == ']')
                        {
                            ProcessToken(sb, opString, i, j, ref cursor);

                            i = j;
                            break;
                        }
                    }
                    if (j == len) //we got to the end of opString without seeing another [ or ]
                    {
                        sb.Append(opString.Substring(i, j - i));
                        break;
                    }
                }
                else
                {
                    sb.Insert(cursor, opString[i]);
                    cursor++;
                }
            }

            return sb.ToString();
        }

        private static void ProcessToken(StringBuilder sb, string opString, int i, int j, ref int cursor)
        {
            string sub;
            if (SpecialKeys.Contains(sub = opString.Substring(i, j - i + 1)) == false)
            {

                sb.Insert(cursor, sub);
                cursor += sub.Length;
            }
            else if (sub == "[LEFT]")
            {
                if (cursor - 1 >= 0)
                    cursor--;
            }
            else if (sub == "[RIGHT]")
            {
                if (cursor + 1 <= sb.Length)
                    cursor++;
            }
            else if (sub == "[DELETE]")
            {
                if (cursor < sb.Length)
                    sb.Remove(cursor, 1);
            }
            else if (sub == "[BACKSPACE]")
            {
                if (cursor > 0)
                {
                    sb.Remove(cursor - 1, 1);
                    cursor--;
                }
            }
        }


    }
}
