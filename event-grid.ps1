Login-AzureRmAccount

$location = "westus2"
$gridSubName = "gridBlobSasha"
$resourceGroup = "event-grid-rg"

New-AzureRmResourceGroup -Name $resourceGroup -Location $location

$storageName = "gridstoragesasha"
$storageAccount = New-AzureRmStorageAccount -ResourceGroupName $resourceGroup `
  -Name $storageName `
  -Location $location `
  -SkuName Standard_LRS `
  -Kind BlobStorage `
  -AccessTier Hot

$ctx = $storageAccount.Context

$binEndPoint = "https://gridfun.azurewebsites.net/api/gridHook?code=/m/YauSHdzMkdiDtcqklWWoFYW3KRrE5CjFGKXTzCbi/Yn7wStOxYw=="

$storageId = (Get-AzureRmStorageAccount -ResourceGroupName $resourceGroup -AccountName $storageName).Id

New-AzureRmEventGridSubscription `
  -EventSubscriptionName $gridSubName `
  -Endpoint $binEndPoint `
  -ResourceId $storageId
  
$containerName = "gridcontainer"
New-AzureStorageContainer -Name $containerName -Context $ctx

echo $null >> gridTestFile.txt

Set-AzureStorageBlobContent -File gridTestFile.txt -Container $containerName -Context $ctx -Blob gridTestFile.txt