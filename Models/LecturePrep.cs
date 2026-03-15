using System;

namespace EduSyncAI
{
    public class LecturePrep
    {
        public int Id { get; set; }
        public int LectureId { get; set; }
        public string CoreIdeas { get; set; }
        public string KeyTerms { get; set; }
        public string SimpleExample { get; set; }
        public string WhatToListenFor { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
