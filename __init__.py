import indydcp_client as client
import socket
import sys


def main():
    # robot connect
    robot_ip = sys.argv[1]
    #robot_ip = "192.168.0.78"
    robot_name = "NRMK-Indy7"
    # indy = robot control
    indy = client.IndyDCPClient(robot_ip, robot_name)

    # Start Server On
    HOST = '127.0.0.1'
    PORT = 8888

    # 1. open Socket
    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    print('Socket created')

    # 2. bind to a address and port
    try:
        s.bind((HOST, PORT))
    except socket.error as msg:
        print('Bind Failed. Error code: ' + str(msg[0]) + ' Message: ' + msg[1])
        sys.exit()

    print('Socket bind complete')

    # 3. Listen for incoming connections
    s.listen(1)
    print('Socket now listening')

    while 1:
        # 4. Accept connection
        conn, addr = s.accept()
        print('connect with client')

        try:
            indy.connect()
            indy.set_reduced_mode(0)
            indy.set_reduced_mode(1)
            # 5. data streaming
            while 1:
                t_pos = indy.get_task_pos()
                x = round(1000 * t_pos[0], 2)
                y = round(1000 * t_pos[1], 2)
                z = round(1000 * t_pos[2], 2)
                # print("sending...")
                data = str(x) + "," + str(y) + "," + str(z)
                conn.sendall(data.encode('utf-8'))
                rec = conn.recv(1024)
                rec = rec.decode('utf-8')
                if (rec == "Stop"):
                    #print("Stop!")
                    indy.set_reduced_speed_ratio(0)
                elif(rec == "Warn"):
                    #print("Warn! Very Slow")
                    indy.set_reduced_speed_ratio(0.5)
                elif (rec == "Safe"):
                    #print("Safe! Resume")
                    indy.set_reduced_speed_ratio(1)
                elif (rec == "End"):
                    print("Disconnect with Client!")
                    conn.sendall("OK".encode('utf-8'))
                    conn.close()
                    break

        except:
            print("Error socket connect")
            indy.set_reduced_mode(0)
            indy.disconnect()
            return

    indy.set_reduced_mode(0)
    indy.disconnect()
    s.close()

if __name__ == "__main__":
    main()
