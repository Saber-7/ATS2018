namespace ATS.Model
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("plans.act_planitem")]
    public partial class act_planitem
    {
        public act_planitem()
        {
            act_selectjiao = new HashSet<act_selectjiao>();
        }

        [Key]
        public int act_PlanId { get; set; }

        [StringLength(45)]
        public string act_PlanName { get; set; }

        public virtual ICollection<act_selectjiao> act_selectjiao { get; set; }
    }
}
