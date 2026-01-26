CREATE DATABASE JJGNet
    ON
    ( NAME = JJGNet_Data,
        FILENAME = '/var/opt/mssql/data/jjgnet.mdf',
        SIZE = 10,
        MAXSIZE = 50,
        FILEGROWTH = 5 )
    LOG ON
    ( NAME = JJGNet_Log,
        FILENAME = '/var/opt/mssql/data/jjgnet.ldf',
        SIZE = 5MB,
        MAXSIZE = 25MB,
        FILEGROWTH = 5MB ) ;
GO

--- Replace <REPLACE_ME> with real password
USE master
CREATE Login jjgnet_user
    WITH Password='5cEZpbhz&p5i&DaA2*N68Nn4sJINd2'
GO

USE JJGNet
CREATE USER jjgnet_user FOR LOGIN jjgnet_user;

ALTER ROLE db_datareader ADD MEMBER jjgnet_user;
ALTER ROLE db_datawriter ADD MEMBER jjgnet_user;
GO
