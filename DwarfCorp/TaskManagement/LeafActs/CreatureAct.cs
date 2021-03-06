﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class CreatureAct : Act
    {
        public CreatureAI Agent { get; set; }
        
        [JsonIgnore]
        public Creature Creature
        {
            get { return Agent.Creature; }
        }
        
        public CreatureAct(CreatureAI agent)
        {
            Agent = agent;
            Name = "Creature Act";
        }

        public CreatureAct()
        {

        }
    }
}