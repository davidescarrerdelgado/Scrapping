//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Data
{
    using System;
    using System.Collections.Generic;
    
    public partial class Enlace_Hijo_Caracteristica
    {
        public int id_detalle_caracteristica { get; set; }
        public Nullable<int> id_enlace_hijo { get; set; }
        public Nullable<int> id_caracteristica { get; set; }
        public string caracteristica_validate { get; set; }
        public Nullable<int> tipo_validate { get; set; }
        public string value { get; set; }
        public Nullable<System.DateTime> sysdate { get; set; }
    }
}