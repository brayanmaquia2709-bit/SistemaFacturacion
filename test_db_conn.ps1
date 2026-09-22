try {
    $prods = Invoke-RestMethod -Uri "http://localhost:5145/api/Productos" -TimeoutSec 5
    Write-Output "SUCCESS: Productos loaded ($($prods.Count) items)"
} catch {
    Write-Output "ERROR Productos: $($_.Exception.Message)"
}

try {
    $clis = Invoke-RestMethod -Uri "http://localhost:5145/api/Clientes" -TimeoutSec 5
    Write-Output "SUCCESS: Clientes loaded ($($clis.Count) items)"
} catch {
    Write-Output "ERROR Clientes: $($_.Exception.Message)"
}

try {
    $usrs = Invoke-RestMethod -Uri "http://localhost:5145/api/Auth/usuarios" -TimeoutSec 5
    Write-Output "SUCCESS: Usuarios loaded ($($usrs.Count) items)"
} catch {
    Write-Output "ERROR Usuarios: $($_.Exception.Message)"
}

try {
    $facs = Invoke-RestMethod -Uri "http://localhost:5145/api/Facturas" -TimeoutSec 5
    Write-Output "SUCCESS: Facturas loaded ($($facs.Count) items)"
} catch {
    Write-Output "ERROR Facturas: $($_.Exception.Message)"
}
