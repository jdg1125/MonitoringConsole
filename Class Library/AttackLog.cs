﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonitoringConsole.Class_Library
{
    public class AttackLog
    {
        public string Username { get; set; }
        public string AttackerIP { get; set; }
        public string WorkspaceId { get; set; }

        public List<string> KeyStrokes { get; set; }
        
    }
}
