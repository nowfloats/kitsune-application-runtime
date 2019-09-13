using Kitsune.Identifier.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Kitsune.Identifier.Helpers
{
    internal class LDRMProcessor
    {
        private readonly RequestDelegate _next;

        public LDRMProcessor(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            try
            {
                var request = context.Request;
                var appRequestUri = new Uri($"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}");

                //TODO: Optimize this OriginDomainArrayStrings()
                if (LDRMProcessor.AllDomainOriginStrings.Contains(appRequestUri.Authority.ToUpper()))
                {
                    var ldrmResponse = LDRMProcessor.HasLegacyMapping(appRequestUri.Authority, appRequestUri);
                    if (ldrmResponse.match)
                    {
                        var builder = new UriBuilder(appRequestUri);
                        try
                        {
                            builder.Host = ldrmResponse.destination_domain;
                            try
                            {
                                builder.Port = ldrmResponse.destination_domain_port;
                                if (builder.Port == 443)
                                    builder.Scheme = "https";

                                appRequestUri = builder.Uri;
                            }
                            catch { }
                        }
                        catch { }

                        //TODOC : Check which object to pass to the function and update function accordingly
                        var httpResponseMessage = await ProxyHelper.CreateProxyHttpRequest(request, appRequestUri);

                        if (httpResponseMessage.IsSuccessStatusCode)
                        {
                            var contentType = httpResponseMessage.Content.Headers.ContentType.MediaType;
                            context.Response.StatusCode = (int)httpResponseMessage.StatusCode;

                            foreach (var header in httpResponseMessage.Headers)
                            {
                                context.Response.Headers[header.Key] = header.Value.ToArray();
                            }

                            // Remove Caching
                            context.Response.Headers["Cache-Control"] = "max-age=0, no-cache, no-store, must-revalidate";
                            context.Response.Headers["Pragma"] = "no-cache";
                            context.Response.Headers["Expires"] = "0";
                            context.Response.ContentType = contentType;

                            if (contentType.StartsWith("text"))
                            {
                                await context.Response.WriteAsync(await httpResponseMessage.Content.ReadAsStringAsync());
                            }
                            else
                            {
                                Byte[] data = await httpResponseMessage.Content.ReadAsByteArrayAsync();
                                await context.Response.Body.WriteAsync(data, 0, data.Length);
                            }
                            return;
                        }
                    }
                }
            }
            catch { }

            await _next.Invoke(context);
        }

        #region Processig LDRM Requests
        public static List<LDRM_Rule> LDRM_Rules = new List<LDRM_Rule> {
            new LDRM_Rule
            {
                origin_domain_host = "5A7C45B47F22D3051AB3D1B5.DEMO.GETKITSUNE.COM",
                destination_domain = "APIUAT.RELIGAREHEALTHINSURANCE.COM",
                destination_domain_port = 443,
                rules = new [] {new Regex(".php"), new Regex("/group-explore-filldetails"), new Regex("/group-care-filldetails"), new Regex("/group-secure-filldetails"), new Regex("/get-claims-tracking-open-cases"), new Regex("/MSCRMController"), new Regex("/SelfHelpController"), new Regex("/aadhaarController") }
            },
            new LDRM_Rule
            {
                origin_domain_host = "RELIGARE.GETKITSUNE.COM",
                destination_domain = "API.RELIGAREHEALTHINSURANCE.COM",
                destination_domain_port = 443,
                rules = new [] {new Regex(".php"), new Regex("/group-explore-filldetails"), new Regex("/group-care-filldetails"), new Regex("/group-secure-filldetails"), new Regex("/get-claims-tracking-open-cases"), new Regex("/MSCRMController"), new Regex("/SelfHelpController"), new Regex("/aadhaarController") }
            },
             new LDRM_Rule
            {
                origin_domain_host = "WWW.RELIGAREHEALTHINSURANCE.COM",
                destination_domain = "API.RELIGAREHEALTHINSURANCE.COM",
                destination_domain_port = 443,
                rules = new [] {new Regex(".php"), new Regex("/group-explore-filldetails"), new Regex("/group-care-filldetails"), new Regex("/group-secure-filldetails"), new Regex("/get-claims-tracking-open-cases"), new Regex("/MSCRMController"), new Regex("/SelfHelpController"), new Regex("/aadhaarController") }
            },
             new LDRM_Rule
            {
                origin_domain_host = "RELIGAREUAT.GETKITSUNE.COM",
                destination_domain = "WWW.RELIGAREHEALTHINSURANCE.COM",
                destination_domain_port = 443,
                rules = new [] {new Regex(".php"), new Regex("/group-explore-filldetails"), new Regex("/group-care-filldetails"), new Regex("/group-secure-filldetails"), new Regex("/get-claims-tracking-open-cases"), new Regex("/MSCRMController"), new Regex("/SelfHelpController"), new Regex("/aadhaarController") }
             },
             new LDRM_Rule
            {
                origin_domain_host = "RHICLUAT.RELIGAREHEALTHINSURANCE.COM",
                destination_domain = "WWW.RELIGAREHEALTHINSURANCE.COM",
                destination_domain_port = 443,
                rules = new [] {new Regex(".php"), new Regex("/group-explore-filldetails"), new Regex("/group-care-filldetails"), new Regex("/group-secure-filldetails"), new Regex("/get-claims-tracking-open-cases"), new Regex("/MSCRMController"), new Regex("/SelfHelpController"), new Regex("/aadhaarController") }
             },
            new LDRM_Rule
            {
                origin_domain_host = "RELIGAREUAT.RELIGAREHEALTHINSURANCE.COM",
                destination_domain = "APIUAT.RELIGAREHEALTHINSURANCE.COM",
                destination_domain_port = 443,
                rules = new [] {new Regex(".php"), new Regex("/group-explore-filldetails"), new Regex("/group-care-filldetails"), new Regex("/group-secure-filldetails"), new Regex("/get-claims-tracking-open-cases"), new Regex("/MSCRMController"), new Regex("/SelfHelpController"), new Regex("/aadhaarController") }
            },
            new LDRM_Rule
            {
                origin_domain_host = "RHICLKITSUNEUAT.RELIGAREHEALTHINSURANCE.COM",
                destination_domain = "APIKITSUNEUAT.RELIGAREHEALTHINSURANCE.COM",
                methods = new List<string>{ "POST" },
                destination_domain_port = 443,
                rules = new [] {
                    new Regex(".php"),
                    new Regex(".*\\.html.*(\\?[^v=]|\\&[^v=])")
                }
            },
            new LDRM_Rule
            {
                origin_domain_host = "5AB473D942FAB004FE2B5183.DEMO.GETKITSUNE.COM",
                destination_domain = "APIKITSUNEUAT.RELIGAREHEALTHINSURANCE.COM",
                methods = new List<string>{ "POST" },
                destination_domain_port = 443,
                rules = new [] {
                    new Regex(".php"),
                    new Regex(".*\\.html.*(\\?[^v=]|\\&[^v=])")
                }
            }
        };

        public static List<string> AllDomainOriginStrings = new List<string>();

        public static Dictionary<string, LDRM_Rule> All_LDRM_Validation_Rules = new Dictionary<string, LDRM_Rule>();

        static LDRMProcessor()
        {
            try
            {
                AllDomainOriginStrings = (from ldrmRule in LDRM_Rules select ldrmRule.origin_domain_host.ToUpper())?.ToList();
            }
            catch { }

            try
            {
                foreach (var ldrmRule in LDRM_Rules)
                {
                    All_LDRM_Validation_Rules.Add(ldrmRule.origin_domain_host.ToUpper(), ldrmRule);
                }
            }
            catch { }
        }

        //TODO: move to dictionary
        //public static string[] OriginDomainArrayStrings()
        //{
        //    try
        //    {
        //        return (from ldrmRule in LDRM_Rules select ldrmRule.origin_domain_host.ToUpper()).ToArray();
        //    }
        //    catch (Exception)
        //    {
        //        return new[] { "" };
        //    }
        //}

        public static LDRMResponse MappingDetails(string hostName)
        {
            try
            {
                if (!String.IsNullOrEmpty(hostName))
                {
                    hostName = hostName.ToUpper();

                    var ldrmRule = All_LDRM_Validation_Rules[hostName];

                    return new LDRMResponse { match = true, destination_domain = ldrmRule.destination_domain, destination_domain_port = ldrmRule.destination_domain_port };
                }
            }
            catch (Exception ex)
            {

            }
            return new LDRMResponse { match = false };
        }

        public static LDRMResponse HasLegacyMapping(string hostName, Uri uri, string method = "GET")
        {
            try
            {
                if (!String.IsNullOrEmpty(hostName))
                {
                    hostName = hostName.ToUpper();

                    var ldrmRule = All_LDRM_Validation_Rules[hostName];

                    if (ldrmRule.methods != null && ldrmRule.methods.Any())
                    {
                        method = method.ToUpper();
                        if (ldrmRule.methods.Contains(method))
                        {
                            return new LDRMResponse { match = true, destination_domain = ldrmRule.destination_domain, destination_domain_port = ldrmRule.destination_domain_port };
                        }
                    }

                    var validationRules = ldrmRule.rules;

                    var rule_match = false;
                    var destination_uri = string.Empty;

                    foreach (var _rule in validationRules)
                    {
                        if (_rule.IsMatch(uri.ToString()))
                        {
                            rule_match = true;
                            break;
                        }
                    }

                    return new LDRMResponse { match = rule_match, destination_domain = ldrmRule.destination_domain, destination_domain_port = ldrmRule.destination_domain_port };
                }
            }
            catch (Exception ex)
            {
                //Handle exception
            }
            return new LDRMResponse { match = false };
        }

        internal static LDRMResponse HasSettingBasedLegacyMapping(Uri uri, dynamic ldrmSettings, string method = "GET")
        {
            try
            {
                if (ldrmSettings.rules != null)
                {
                    string domain = ldrmSettings.destination_domain?.Value?.ToString();
                    string portString = ldrmSettings.destination_domain_port?.Value?.ToString();
                    int port = 80;
                    int.TryParse(portString, out port);
                    foreach (dynamic rule in ldrmSettings.rules)
                    {
                        Regex ruleRegex = new Regex(rule.Value);
                        if (ruleRegex.IsMatch(uri.ToString()))
                        {
                            return new LDRMResponse { match = true, destination_domain = domain, destination_domain_port = port };
                        }
                    }
                }
            }
            catch (Exception e) { }
            return new LDRMResponse { match = false };
        }
        #endregion
    }

    #region LDRM Models
    internal class LDRM_Rule
    {
        internal string origin_domain_host { get; set; }
        internal string destination_domain { get; set; }
        internal int destination_domain_port { get; set; }
        internal Regex[] rules { get; set; }
        internal List<string> methods { get; set; }
    }

    internal class LDRMResponse
    {
        internal bool match;
        internal string destination_domain;
        internal int destination_domain_port;
    }
    #endregion 
}
