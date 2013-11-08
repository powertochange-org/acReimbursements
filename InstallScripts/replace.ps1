Param(
  [string]$webConfig
)
# $webConfig = "C:\aDev\acDev1\web.config"
   $doc = new-object System.Xml.XmlDocument
   $doc.Load($webConfig)
   
   $codeSubDirectories = $doc.SelectSingleNode('//system.web/compilation/codeSubDirectories')
   
   if(-not $codeSubDirectories)
   {
   $compilation = $doc.SelectSingleNode("//system.web/compilation")
   $codeSubDirectories = $doc.CreateElement("codeSubDirectories")
   $compilation.AppendChild($codeSubDirectories)
   }
   
   $exists = "NO"
    FOREACH ($j in $codeSubDirectories.ChildNodes)
	{
		if ($j.directoryName -eq "StaffRmb"){$exists="YES";}
   } 
   if($exists -eq "NO")
   {
	$staffRmb = $doc.CreateElement("add")
	$xmlAttr1 = $doc.CreateAttribute("directoryName")
	$xmlAttr1.Value = "StaffRmb"
	$staffRmb.Attributes.Append($xmlAttr1)
	$codeSubDirectories.AppendChild($staffRmb)
   }
    
   
  
  $doc.Save($webConfig)