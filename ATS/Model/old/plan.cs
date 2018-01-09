namespace ATS.Model
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("plans.plan")]
    public partial class plan
    {
        public int Id { get; set; }

        public int JiaoId { get; set; }

        [StringLength(45)]
        public string JiaoName { get; set; }

        [StringLength(45)]
        public string StaName { get; set; }

        public TimeSpan? ReachTime { get; set; }

        public TimeSpan? LeaveTime { get; set; }

        public int? Distance { get; set; }

        public virtual selectjiao selectjiao { get; set; }
    }
}
