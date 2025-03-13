using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
namespace signalRSinServicio.Servicios;

   

    public class SqLiteService
    {
        private readonly string _connectionString;

        public SqLiteService(string databaseFile, string password)
        {
        _connectionString = $"Data Source={databaseFile}";
        SQLitePCL.Batteries_V2.Init(); // Inicializa SQLCipher
        CreateDatabaseAndTable(password);
    }

        private void CreateDatabaseAndTable(string password)
        {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            SetPassword(connection, password);

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS clientes (
                    idcliente INTEGER PRIMARY KEY AUTOINCREMENT,
                    GUID TEXT NOT NULL,
                    bToken TEXT NOT NULL,
                    email TEXT,
                    password TEXT NOT NULL,
                    fecha_registro DATETIME DEFAULT CURRENT_TIMESTAMP
                );
            ";
            command.ExecuteNonQuery();

            Console.WriteLine("Tabla 'clientes' creada exitosamente.");
            // Verificar si la tabla ya tiene registros
            command.CommandText = "SELECT COUNT(*) FROM clientes";
            var count = Convert.ToInt32(command.ExecuteScalar());

            // Insertar el cliente de ejemplo solo si no hay registros
            if (count == 0)
            {
                InsertCliente("guid-ejemplo-1234", "token-ejemplo-1234", "ands", "1234");
            }
        }
        }
    private void SetPassword(SqliteConnection connection, string password)
    {
        var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA key = '{password}';";
        command.ExecuteNonQuery();
    }
    public void InsertCliente(string guid, string bToken, string email, string password)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            SetPassword(connection, "tu-clave-secreta");

            // Hash de la contraseña
            var hashedPassword = HashPassword(password);

            var command = connection.CreateCommand();
            command.CommandText = @"
                    INSERT INTO clientes (GUID, bToken, email, password)
                    VALUES ($guid, $bToken, $email, $hashedPassword)
                ";
            command.Parameters.AddWithValue("$guid", guid);
            command.Parameters.AddWithValue("$bToken", bToken);
            command.Parameters.AddWithValue("$email", email);
            command.Parameters.AddWithValue("$hashedPassword", hashedPassword);

            command.ExecuteNonQuery();

            Console.WriteLine("Cliente insertado exitosamente.");
        }
    }
    private string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder builder = new StringBuilder();
            foreach (byte b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }
    }
    public class Cliente
    {
        public int IdCliente { get; set; }
        public string GUID { get; set; }
        public string BToken { get; set; }
        public string Password { get; set; }
       
        public string Email { get; set; }
        public DateTime FechaRegistro { get; set; }
    }
    public List<Cliente> DameClientes()
    {
        var clientes = new List<Cliente>();

        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            SetPassword(connection, "Helouda");

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT idcliente, GUID, bToken, email, fecha_registro
                FROM clientes;";

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var cliente = new Cliente
                    {
                        IdCliente = reader.GetInt32(0),
                        GUID = reader.GetString(1),
                        BToken = reader.GetString(2),
                        Email = reader.GetString(3),
                        FechaRegistro = reader.GetDateTime(4)
                    };

                    clientes.Add(cliente);
                }
            }
        }

        return clientes;
    }
    public Cliente VerificaCliente(string email, string pass)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            SetPassword(connection, "Helouda");

            // Hash de la contraseña proporcionada
            var hashedPassword = HashPassword(pass);

            var command = connection.CreateCommand();
            command.CommandText = @"
                    SELECT idcliente, GUID, bToken, email, password, fecha_registro
                    FROM clientes
                    WHERE email = $email AND password = $hashedPassword;
                ";
            command.Parameters.AddWithValue("$email", email);
            command.Parameters.AddWithValue("$hashedPassword", hashedPassword);

            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    var cliente = new Cliente
                    {
                        IdCliente = reader.GetInt32(0),
                        GUID = reader.GetString(1),
                        BToken = reader.GetString(2),
                        Email = reader.GetString(3),
                        Password = reader.GetString(4),
                        FechaRegistro = reader.GetDateTime(5)
                    };

                    return cliente;
                }
            }
        }

        // Devolver un cliente vacío si no existe
        return new Cliente();
    }
}
