namespace ATS.Model
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("plans.planitem")]
    public partial class planitem
    {
        public planitem()
        {
            selectjiao = new HashSet<selectjiao>();
        }

        [Key]
        public int PlanId { get; set; }

        [StringLength(45)]
        public string PlanName { get; set; }

        public virtual ICollection<selectjiao> selectjiao { get; set; }
    }
}
