using SIARAWEB.Models;
using System;
using System.Collections.Generic;

namespace SIARAWEB.ViewModel
{
    public class SubjectHistoryViewModel
    {
        public Subject Subject { get; set; }
        public ApplicationUser Docente { get; set; }
        public List<TaskHistoryGroup> TaskGroups { get; set; } = new List<TaskHistoryGroup>();
    }

    public class TaskHistoryGroup
    {
        public DocumentTask Task { get; set; }
        public List<Document> Documents { get; set; } = new List<Document>();
        public List<AcademicTracking> Trackings { get; set; } = new List<AcademicTracking>();
    }
}