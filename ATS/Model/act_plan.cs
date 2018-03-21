namespace ATS.Model
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("plans.act_plan")]
    public partial class act_plan
    {
        public int Id { get; set; }

        public int act_JiaoId { get; set; }

        [StringLength(45)]
        public string act_JiaoName { get; set; }

        [StringLength(45)]
        public string act_StaName { get; set; }

        public TimeSpan? act_ReachTime { get; set; }

        public TimeSpan? act_LeaveTime { get; set; }

        public int? act_Distance { get; set; }

        public virtual act_selectjiao act_selectjiao { get; set; }
    }
}
