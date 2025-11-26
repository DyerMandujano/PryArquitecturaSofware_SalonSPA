using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using Pry_Solu_SalonSPA.Models;

namespace Pry_Solu_SalonSPA.ViewModels
{
    // LISTADO
    public class CompraListaVM
    {
        public int Id_Compra { get; set; }
        public DateTime Fecha_compra { get; set; }
        public string Nom_Prove { get; set; }
        public string Tipo_Doc { get; set; }
        public int Total_Productos { get; set; }
        public decimal Monto_Total { get; set; }
    }


    // REGISTRAR COMPRA - CORREGIDO
    public class CompraRegistrarVM
    {
        public DateTime FechaCompra { get; set; } //compra
        public int IdProveedor { get; set; } //compra 
        public string TipoDoc { get; set; } //compra
        public int IdCompra { get; set; } //compra 
        public decimal MontoTotal { get; set; }//monto calculado
        [ValidateNever]
        public List<Proveedor> Proveedor { get; set; }  // Para el select de proveedores
        [ValidateNever]
        public List<Producto> Productos { get; set; }     // Para el select de productos
        public List<CompraDetalleVM> Detalles { get; set; }// Para recibir los detalles desde el for
    }

    public class CompraDetalleVM
    {
        public int IdProducto { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioCompra { get; set; }
    }


    public class CompraDetallePorCompraVM
    {
        public int IdCompra { get; set; }
        public DateTime FechaCompra { get; set; }
        public string Proveedor { get; set; }
        public string TipoDoc { get; set; }
        public decimal MontoTotal { get; set; }
        public List<CompraDetalleItemVM> DetallesItem { get; set; }
    }

    public class CompraDetalleItemVM
    {
        public int IdProducto { get; set; }
        public string Producto { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioCompra { get; set; }

        public decimal SubTotal => Cantidad * PrecioCompra;
    }




}
