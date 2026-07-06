import os
import json
import pyodbc
import sys

def load_config():
    """Loads database configuration from JSON file."""
    config_path = os.path.join(os.path.dirname(__file__), 'db_config.json')
    if not os.path.exists(config_path):
        print(f"Error: No se encontró el archivo de configuración {config_path}")
        sys.exit(1)
    with open(config_path, 'r', encoding='utf-8') as f:
        return json.load(f)

def get_connection(config):
    """Establishes database connection using configuration parameters."""
    # Intentamos primero con Autenticación de SQL Server
    conn_str_sql = (
        f"Driver={{{config['Driver']}}};"
        f"Server={config['Server']};"
        f"Database={config['Database']};"
        f"UID={config['UID']};"
        f"PWD={config['PWD']};"
    )
    
    # Connection string alternativa con Autenticación de Windows (Fallback)
    conn_str_win = (
        f"Driver={{{config['Driver']}}};"
        f"Server={config['Server']};"
        f"Database={config['Database']};"
        f"Trusted_Connection=yes;"
    )
    
    try:
        # Intento de conexión con el Login creado (saadmin5482)
        print("Intentando conectar con login 'saadmin5482'...")
        conn = pyodbc.connect(conn_str_sql)
        print("Conexión establecida con Autenticación de SQL Server (saadmin5482).")
        return conn
    except pyodbc.Error as e:
        # Si falla debido a que el servidor está en modo "Solo Windows Authentication"
        print("\n[Aviso] No se pudo conectar usando Autenticación de SQL Server.")
        print(f"Detalle: {e}")
        print("\nIntentando conectar con Autenticación de Windows (Trusted Connection) como fallback...")
        try:
            conn = pyodbc.connect(conn_str_win)
            print("Conexión establecida exitosamente mediante Autenticación de Windows.")
            return conn
        except pyodbc.Error as e_win:
            print(f"Error crítico: No se pudo establecer conexión de ninguna forma.")
            print(f"Detalle: {e_win}")
            sys.exit(1)

def menu_consultar(conn):
    """Option 1: Consult loans of a user by calling the stored procedure."""
    print("\n==========================================")
    print("      CONSULTAR PRÉSTAMOS DE USUARIO")
    print("==========================================")
    try:
        id_usuario = int(input("Ingrese el ID del Usuario a consultar: "))
    except ValueError:
        print("Error: El ID debe ser un número entero.")
        return

    try:
        cursor = conn.cursor()
        # Llamar al Store Procedure
        query = "{CALL Reportes.spconsultaprestamo_equipo (?)}"
        cursor.execute(query, (id_usuario,))
        rows = cursor.fetchall()
        
        if not rows:
            print(f"\nNo se encontraron préstamos para el IDUsuario: {id_usuario}")
            return
            
        print(f"\nResultados para el IDUsuario {id_usuario}:")
        print("-" * 120)
        print(f"{'IDUsuario':<10} | {'Nombre':<15} | {'Apellido':<15} | {'IDPrestamo':<10} | {'Fecha':<12} | {'Equipo':<20} | {'Ndias':<6} | {'Valor':<10}")
        print("-" * 120)
        for row in rows:
            fecha_str = row.Fecha.strftime('%Y-%m-%d') if hasattr(row.Fecha, 'strftime') else str(row.Fecha)
            valor_float = float(row.Valor) if hasattr(row.Valor, '__float__') else row.Valor
            print(f"{row.IDUsuario:<10} | {row.Nombre:<15} | {row.Apellido:<15} | {row.IDPrestamo:<10} | {fecha_str:<12} | {row.Equipo:<20} | {row.Ndias:<6} | ${valor_float:<9.2f}")
        print("-" * 120)
    except pyodbc.Error as err:
        print(f"Error al ejecutar el Store Procedure: {err}")

def menu_insertar(conn):
    """Option 2: Insert new equipment into GENERAL.Equipos."""
    print("\n==========================================")
    print("        INSERTAR NUEVO EQUIPO")
    print("==========================================")
    try:
        id_equipo = int(input("Ingrese el ID del Equipo (Ej. 4): "))
        nombre = input("Ingrese el Nombre del Equipo (Ej. Monitor LG): ").strip()
        tipo = input("Ingrese el Tipo de Equipo (Ej. Monitor/Laptop): ").strip()
        precio = float(input("Ingrese el Precio por Día (Ej. 5.50): "))
        descripcion = input("Ingrese una Descripción breve: ").strip()
        
        if not nombre:
            print("Error: El nombre del equipo no puede estar vacío.")
            return
    except ValueError:
        print("Error: ID debe ser entero y Precio debe ser decimal.")
        return

    try:
        cursor = conn.cursor()
        query = """
        INSERT INTO GENERAL.Equipos (IDEquipo, NombreEquipo, TipoEquipo, PrecioxDia, Descripcion)
        VALUES (?, ?, ?, ?, ?)
        """
        cursor.execute(query, (id_equipo, nombre, tipo, precio, descripcion))
        conn.commit()
        print(f"\n¡Equipo '{nombre}' (ID: {id_equipo}) insertado exitosamente!")
        
        # Verificar inserción
        cursor.execute("SELECT * FROM GENERAL.Equipos WHERE IDEquipo = ?", (id_equipo,))
        row = cursor.fetchone()
        if row:
            print("\nEvidencia en Base de Datos del equipo creado:")
            print(f"ID: {row.IDEquipo} | Nombre: {row.NombreEquipo} | Tipo: {row.TipoEquipo} | Precio: ${float(row.PrecioxDia):.2f} | Desc: {row.Descripcion}")
            
    except pyodbc.Error as err:
        print(f"Error al insertar el equipo en la BDD: {err}")

def main():
    config = load_config()
    conn = get_connection(config)
    
    while True:
        print("\n" + "="*40)
        print("           SISTEMA RENTING PC")
        print("="*40)
        print("1. Consultar Préstamos de un Usuario (SP)")
        print("2. Insertar Nuevo Equipo (INSERT)")
        print("3. Salir")
        print("="*40)
        
        opcion = input("Seleccione una opción (1-3): ").strip()
        
        if opcion == '1':
            menu_consultar(conn)
        elif opcion == '2':
            menu_insertar(conn)
        elif opcion == '3':
            print("\nCerrando conexión y saliendo del programa. ¡Hasta luego!")
            conn.close()
            break
        else:
            print("\nOpción no válida. Intente de nuevo.")

if __name__ == "__main__":
    main()
