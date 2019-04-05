using IntBank.ConfiguracionTabs.BusinessTypes;
using IntBank.ConfiguracionTabs.DataLayer;
using IntBank.ConfiguracionTabs.ExtensionMethods;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;

namespace IntBank.ConfiguracionTabs
{
    public class Generar : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            try
            {
                ServerConnection cnx = new ServerConnection(serviceProvider);
                Entity entity = cnx.context.InputParameters["Target"] as Entity;

                if (!ValidacionContexto(cnx, entity))
                    return;

                CrmRepository crmRepository = new CrmRepository(cnx);
                List<Tab> tabs = crmRepository.RecuperarTabs(entity.Id);
                tabs.ForEach(t => t.Fichas = crmRepository.RecuperarFichas(t.Id));
                foreach (var tab in tabs)
                {
                    tab.Fichas.ForEach(f => f.Secciones = crmRepository.RecuperarSecciones(f.Id));
                }

                entity["rs_configuraciontabs"] = ContruirCadenaConfiguracionesTabs(tabs);
                entity["rs_publicarconfiguracion"] = false;
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException($"IntBank.ConfiguracionTabs.Generar: " + ex.Message);
            }
        }

        private bool ValidacionContexto(ServerConnection cnx, Entity entity)
        {
            if (cnx.context.MessageName.ToLower() != "update")
                return false;

            if (!entity.GetBoolValue("rs_publicarconfiguracion"))
                return false;

            return true;
        }

        public string ContruirCadenaConfiguracionesTabs(List<Tab> tabs)
        {
            string configuracion = string.Empty;

            foreach (var tab in tabs)
            {
                foreach (var ficha in tab.Fichas)
                {
                    configuracion += "|";
                    configuracion += tab.Etiqueta + "," + tab.Orden + "," + ficha.NombreEsquema + ",";
                    ficha.Secciones.ForEach(s => configuracion += s + "/");
                    configuracion = configuracion.TrimEnd('/');
                }
                //configuracion += "|";
            }
            if (configuracion.StartsWith("|"))
                configuracion = configuracion.Substring(1, configuracion.Length - 1);
            configuracion = configuracion.TrimEnd('|');

            return configuracion;
        }
    }
}
