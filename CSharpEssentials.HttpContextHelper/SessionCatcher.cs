using Microsoft.AspNetCore.Http;

namespace CSharpEssentials.HttpContextHelper;
public class SessionCatcher {
    public string ipAddress { get; set; }
    public SessionCatcher(HttpContext context) {
        if (context.Request.Headers.TryGetValue("HTTP_X_FORWARDED_FOR", out var forwardedIps))
            ipAddress = forwardedIps.First();
        else
            context.Request.Headers.TryGetValue("REMOTE_ADDR", out var ipAddress);
        if (string.IsNullOrEmpty(ipAddress) && context.Request.Headers["X-Real-IP"].Count() > 0)
            ipAddress = context.Request.Headers["X-Real-IP"].ToString();
        if (string.IsNullOrEmpty(ipAddress))
            ipAddress = context.Connection.RemoteIpAddress.ToString();
    }
    /// <summary> 
    /// Compares an IP address to list of valid IP addresses attempting to 
    /// find a match 
    /// </summary> 
    /// <param name="ipAddress">String representation of a valid IP Address</param> 
    /// <returns></returns> 
    public bool IsIpAddressValid(string ipAddress, string addresses) {
        //Split the users IP address into it's 4 octets (Assumes IPv4) 
        string[] incomingOctets = ipAddress.Trim().Split(new char[] { '.' });
        if (addresses == null) {
            return true;
        }
        //Store each valid IP address in a string array 
        string[] validIpAddresses = addresses.Trim().Split(new char[] { ',' });

        //Iterate through each valid IP address 
        foreach (var validIpAddress in validIpAddresses) {
            //Return true if valid IP address matches the users 
            if (validIpAddress.Trim() == ipAddress) {
                return true;
            }

            //Split the valid IP address into it's 4 octets 
            string[] validOctets = validIpAddress.Trim().Split(new char[] { '.' });

            bool matches = true;

            //Iterate through each octet 
            for (int index = 0; index < validOctets.Length; index++) {
                //Skip if octet is an asterisk indicating an entire 
                //subnet range is valid 
                if (validOctets[index] != "*") {
                    if (validOctets[index] != incomingOctets[index]) {
                        matches = false;
                        break; //Break out of loop 
                    }
                }
            }

            if (matches) {
                return true;
            }
        }

        //Found no matches 
        return false;
    }
}
