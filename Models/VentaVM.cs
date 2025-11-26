using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using Pry_Solu_SalonSPA.Models;

namespace Pry_Solu_SalonSPA.ViewModels
{
    public class VentaListaVM
    {
        public int Id_Venta { get; set; }
        public DateTime Fecha_Venta { get; set; }
        public string Nombre_Cliente { get; set; }  // Viene concatenado del SP
        public string Nombre_Empleado { get; set; } // Viene concatenado del SP
        public string Metodo_Pago { get; set; }
        public string Tipo_Comprobante { get; set; }
        public int Total_Productos { get; set; }
        public int Total_Servicios { get; set; }
        public decimal Monto_Total { get; set; }
    }

    public class VentaRegistrarVM
    {
        // Búsqueda de Cita
        public int? IdCitaServicioBusqueda { get; set; }

        // Cabecera de venta
        public DateTime FechaVenta { get; set; } = DateTime.Now;
        public int? IdCliente { get; set; } // Puede venir de la cita
        public int IdEmpleado { get; set; }
        public int IdTipoPago { get; set; }
        public int IdTipoComprobante { get; set; }

        // Datos del Cliente (para mostrar)
        public string NombreCliente { get; set; }
        public string TelefonoCliente { get; set; }
        public string DniCliente { get; set; }

        public int IdPersona { get; set; }

        // Listas para dropdowns (se inicializan para evitar nulls)
        [ValidateNever]
        public List<Empleado> Empleados { get; set; } = new List<Empleado>();
        [ValidateNever]
        public List<TipoPago> TiposPago { get; set; } = new List<TipoPago>();
        [ValidateNever]
        public List<TipoComprobante> TiposComprobante { get; set; } = new List<TipoComprobante>();
        [ValidateNever]
        public List<Producto> Productos { get; set; } = new List<Producto>();
        [ValidateNever]
        public List<Cliente> Clientes { get; set; } = new List<Cliente>(); // NUEVO

        public virtual Persona IdPersonaNavigation { get; set; } = null!;
        // Detalles (pueden ser servicios de cita y/o productos)
        public List<VentaDetalleVM> Detalles { get; set; } = new List<VentaDetalleVM>();
    }

    public class VentaDetalleVM
    {
        public string TipoItem { get; set; } // "Servicio" o "Producto"
        public int? IdCitaServicio { get; set; } // Para servicios
        public int? IdProducto { get; set; } // Para productos
        public string NombreItem { get; set; }
        public int Cantidad { get; set; } = 1;
        public decimal PrecioUnitario { get; set; }
        public decimal SubTotal => Cantidad * PrecioUnitario;

        // Solo para productos (validar stock)
        public int? StockDisponible { get; set; }
    }

    // Para la respuesta de búsqueda de cita
    public class CitaServicioBusquedaVM
    {
        public int Id_CitaServicio { get; set; }
        public int Id_Cliente { get; set; }
        public string Nombre_Cliente { get; set; }
        public string Telefono { get; set; }
        public string Dni { get; set; }
        public int Id_Servicio { get; set; }
        public string Nombre_Servicio { get; set; }
        public string Descripcion_Servicio { get; set; }
        public decimal Precio_Servicio { get; set; }
    }


}