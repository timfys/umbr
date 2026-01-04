using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.Common.Controllers;

public class DownloadApiController : UmbracoApiController
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private readonly IConfiguration _configuration;
    public DownloadApiController(IConfiguration configuration){
        _configuration = configuration;
    }
    // TODO: проверь реальный endpoint из WSDL
    private string ServiceUrl => _configuration["BusinessService:ServiceUrl"];

    // Константы из задания
    private const int OlEntityId = 31159;
    private string OlUserName => _configuration["BusinessService:OlUserName"];
    private string OlPassword => _configuration["BusinessService:OlPassword"];
    private const int BusinessId = 1;
    private const int CategoryId = 150;
    private const int TableId = 99;

    public class DownloadRequest
    {
        public string Name { get; set; }      // из поля name
        public string Phone { get; set; }     // из поля phone
        public string Email { get; set; }     // из поля email
        public string CountryIso { get; set; } = "RU"; // можно подставлять по языку
        public string FileKey { get; set; }   // callcenterV7 (из URL)
    }

    public class DownloadResponse
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        public int EntityId { get; set; }
        public int RecordId { get; set; }
        public int OutgoingResultCode { get; set; }
    }

    private class EntityAddResult
    {
        public int ResultCode { get; set; }
        public string ResultMessage { get; set; }
        public int EntityId { get; set; }
        public int AffiliateResultCode { get; set; }
        public int ExecuteTime { get; set; }
    }

    private class CustomFieldsResult
    {
        public int ResultCode { get; set; }
        public string ResultMessage { get; set; }
        public int recordId { get; set; }
        public int ExecuteTime { get; set; }
    }
    private class OutgoingResult
    {
        public int ResultCode { get; set; }
        public string ResultMessage { get; set; }
        public int recordId { get; set; }
        public int ExecuteTime { get; set; }
    }

    [HttpPost]
    public async Task<ActionResult<DownloadResponse>> Submit([FromBody] DownloadRequest req)
    {
        if (req == null ||
            string.IsNullOrWhiteSpace(req.Name) ||
            string.IsNullOrWhiteSpace(req.Phone) ||
            string.IsNullOrWhiteSpace(req.Email) ||
            string.IsNullOrWhiteSpace(req.FileKey))
        {
            return BadRequest(new DownloadResponse
            {
                Success = false,
                Error = "Invalid data"
            });
        }

        try
        {
            var (entityId, resultCode) = await CallEntityAddAsync(req);

            var ip = GetClientIp();
            
            var recordId = await CallCustomFieldsUpdateAsync(entityId, req.FileKey, ip);
            var outgoingResultCode = await CallOutgoingAddAsync(entityId, 3);
            //outgoingResultCode = await CallOutgoingAddAsync(entityId, 6);
            if(resultCode<0)
                outgoingResultCode = await CallOutgoingAddAsync(entityId, 8);
            return Ok(new DownloadResponse
            {
                Success = true,
                EntityId = entityId,
                RecordId = recordId,
                OutgoingResultCode = outgoingResultCode
            });
        }
        catch (Exception ex)
        {
            return Ok(new DownloadResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    private string GetClientIp()
    {
        var ctx = HttpContext;
        if (ctx.Request.Headers.TryGetValue("CF-Connecting-IP", out var cfIp))
            return cfIp.ToString();

        if (ctx.Request.Headers.TryGetValue("X-Forwarded-For", out var xff))
            return xff.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

        if (ctx.Request.Headers.TryGetValue("X-Real-IP", out var realIp))
            return realIp.ToString();

        return ctx.Connection.RemoteIpAddress?.ToString();
    }

    private async Task<(int entityId, int resultCode)> CallEntityAddAsync(DownloadRequest req)
    {
        // Пароль 6 цифр
        var rnd = new Random();
        var randomPassword = rnd.Next(100000, 999999).ToString();

        // Твоё поле Name → FirstName, LastName можно пустым или таким же
        var firstName = System.Security.SecurityElement.Escape(req.Name);
        var lastName = "";

        var email = System.Security.SecurityElement.Escape(req.Email);
        var mobile = System.Security.SecurityElement.Escape(req.Phone);
        var countryIso = System.Security.SecurityElement.Escape(req.CountryIso);

        var soapEnvelope = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<SOAP-ENV:Envelope xmlns:SOAP-ENV=""http://schemas.xmlsoap.org/soap/envelope/""
                   xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
                   xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                   xmlns:SOAP-ENC=""http://schemas.xmlsoap.org/soap/encoding/""
                   xmlns:NS1=""urn:BusinessApiIntf-IBusinessAPI"">
  <SOAP-ENV:Body SOAP-ENV:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">
    <NS1:Entity_Add>
      <ol_EntityId xsi:type=""xsd:int"">{OlEntityId}</ol_EntityId>
      <ol_UserName xsi:type=""xsd:string"">{OlUserName}</ol_UserName>
      <ol_Password xsi:type=""xsd:string"">{OlPassword}</ol_Password>
      <BusinessId xsi:type=""xsd:int"">{BusinessId}</BusinessId>
      <Employee_EntityId xsi:type=""xsd:int"">0</Employee_EntityId>
      <CategoryID xsi:type=""xsd:int"">{CategoryId}</CategoryID>
      <Email xsi:type=""xsd:string"">{email}</Email>
      <Password xsi:type=""xsd:string"">{randomPassword}</Password>
      <FirstName xsi:type=""xsd:string"">{firstName}</FirstName>
      <LastName xsi:type=""xsd:string"">{lastName}</LastName>
      <Mobile xsi:type=""xsd:string"">{mobile}</Mobile>
      <CountryISO xsi:type=""xsd:string"">{countryIso}</CountryISO>
      <affiliate_entityID xsi:type=""xsd:int"">0</affiliate_entityID>
    </NS1:Entity_Add>
  </SOAP-ENV:Body>
</SOAP-ENV:Envelope>";

        var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
        // Часто для таких сервисов нужно SOAPAction, если будет 500 – посмотри в WSDL
        // content.Headers.Add("SOAPAction", "urn:BusinessApiIntf-IBusinessAPI#Entity_Add");

        var response = await _httpClient.PostAsync(ServiceUrl, content);
        response.EnsureSuccessStatusCode();

        var xml = await response.Content.ReadAsStringAsync();

        var doc = XDocument.Parse(xml);
        var returnNode = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "return");
        if (returnNode == null)
            throw new Exception("Entity_Add: <return> node not found");

        var json = returnNode.Value.Trim();
        var result = JsonSerializer.Deserialize<EntityAddResult>(json);

        if (result == null)
            throw new Exception("Entity_Add: cannot parse JSON");

        // Здесь как раз твои примеры:
        // {"ResultCode":-5674,"ResultMessage":"Customer with these email and mobile is exists","EntityId":49275,...}
        // {"ResultCode":0,"ResultMessage":"OK","EntityId":49276,...}

        if (result.EntityId <= 0)
            throw new Exception($"Entity_Add error: {result.ResultMessage}");
        
        return (result.EntityId, result.ResultCode);
    }

    private async Task<int> CallCustomFieldsUpdateAsync(int entityId, string fileKey, string ip)
    {
        var escapedFileKey = System.Security.SecurityElement.Escape(fileKey);
        var escapedIp = System.Security.SecurityElement.Escape(ip ?? "");

        var soapEnvelope = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<SOAP-ENV:Envelope xmlns:SOAP-ENV=""http://schemas.xmlsoap.org/soap/envelope/""
                   xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
                   xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                   xmlns:SOAP-ENC=""http://schemas.xmlsoap.org/soap/encoding/""
                   xmlns:NS1=""urn:BusinessApiIntf-IBusinessAPI""
                   xmlns:ns2=""urn:CommonWSTypes"">
  <SOAP-ENV:Body SOAP-ENV:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">
    <NS1:CustomFields_Tables_Update>
      <ol_EntityID xsi:type=""xsd:int"">{OlEntityId}</ol_EntityID>
      <ol_Username xsi:type=""xsd:string"">{OlUserName}</ol_Username>
      <ol_Password xsi:type=""xsd:string"">{OlPassword}</ol_Password>
      <TableID xsi:type=""xsd:int"">{TableId}</TableID>
      <NamesArray SOAP-ENC:itemType=""xsd:string"" xsi:type=""ns2:ArrayOfString"">
        <item xsi:type=""xsd:string"">ParentRecordID</item>
        <item xsi:type=""xsd:string"">CustomField100</item>
        <item xsi:type=""xsd:string"">CustomField101</item>
        <item xsi:type=""xsd:string"">customfield102</item>
      </NamesArray>
      <ValuesArray SOAP-ENC:itemType=""xsd:string"" xsi:type=""ns2:ArrayOfString"">
        <item xsi:type=""xsd:string"">{entityId}</item>
        <item xsi:type=""xsd:string"">{escapedFileKey}</item>
        <item xsi:type=""xsd:string"">now()</item>
        <item xsi:type=""xsd:string"">{escapedIp}</item>
      </ValuesArray>
    </NS1:CustomFields_Tables_Update>
  </SOAP-ENV:Body>
</SOAP-ENV:Envelope>";

        var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
        // content.Headers.Add("SOAPAction", "urn:BusinessApiIntf-IBusinessAPI#CustomFields_Tables_Update");

        var response = await _httpClient.PostAsync(ServiceUrl, content);
        response.EnsureSuccessStatusCode();

        var xml = await response.Content.ReadAsStringAsync();

        var doc = XDocument.Parse(xml);
        var returnNode = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "return");
        if (returnNode == null)
            throw new Exception("CustomFields_Tables_Update: <return> node not found");

        var json = returnNode.Value.Trim();
        var result = JsonSerializer.Deserialize<CustomFieldsResult>(json);

        if (result == null)
            throw new Exception("CustomFields_Tables_Update: cannot parse JSON");

        if (result.ResultCode != 0)
            throw new Exception($"CustomFields_Tables_Update error: {result.ResultMessage}");

        return result.recordId;
    }
    
        private async Task<int> CallOutgoingAddAsync(int entityId, int messageType)
        {
        var soapEnvelope = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<env:Envelope xmlns:env=""http://www.w3.org/2003/05/soap-envelope""
 xmlns:ns1=""urn:BusinessApiIntf-IBusinessAPI""
 xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
 xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
 xmlns:enc=""http://www.w3.org/2003/05/soap-encoding""
 xmlns:ns2=""urn:CommonWSTypes"">
<env:Body><ns1:Outgoing_add env:encodingStyle=""http://www.w3.org/2003/05/soap-encoding"">
<ol_EntityID xsi:type=""xsd:int"">{OlEntityId}</ol_EntityID>
<ol_Username xsi:type=""xsd:string"">{OlUserName}</ol_Username>
<ol_Password xsi:type=""xsd:string"">{OlPassword}</ol_Password>
<MessageType xsi:type=""xsd:int"">{messageType}</MessageType>
<MessageID xsi:type=""xsd:int"">1088</MessageID>
<EntityIds enc:itemType=""xsd:int"" enc:arraySize=""1"" xsi:type=""ns2:ArrayOfInt"">
<item xsi:type=""xsd:int"">{entityId}</item></EntityIds>
<NamesArray xsi:nil=""true"" xsi:type=""ns2:ArrayOfString""/>
<ValuesArray xsi:nil=""true"" xsi:type=""ns2:ArrayOfString""/>
</ns1:Outgoing_add></env:Body></env:Envelope>";

        var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");

        var response = await _httpClient.PostAsync(ServiceUrl, content);
        response.EnsureSuccessStatusCode();

        var xml = await response.Content.ReadAsStringAsync();

        var doc = XDocument.Parse(xml);
        var returnNode = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "return");
        if (returnNode == null)
            throw new Exception("<return> node not found");

        var json = returnNode.Value.Trim();
        var result = JsonSerializer.Deserialize<OutgoingResult>(json);

        if (result == null)
            throw new Exception("Outgoing_add: cannot parse JSON");

        if (result.ResultCode != 0)
            throw new Exception($"Outgoing_add error: {result.ResultMessage}");

        return result.ResultCode;
    }
}
