using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PLCSocket.Tools.net.omron.cip
{
    class OmronCipNet
    {
        private String ip;
        private int port = 44818; //默认端口号
        private Socket socket;

        private long seq = 1;
        private static readonly object lockobject= new object();

        /**
         * 协议内容：header(24字节？) - data
         *  	command(2)，length(2)，session(4)，status(4)，sender context(8)，option(4)
         *
         *		data
         *
         *
         */

        /**
         * page 17:
         * command:
         *      0x0004: list services
         *      0x0063: list identity  搜寻目标
         *      0x0065: register
         *      0x0066: unregister
         *      0x006f: send rr data
         *      0x0070: send unit data
         */


        /**
         * length header 后续的长度
         *
         *
         * session： 注册时，目标会返回sessionid，将用于后续的会话中
         *
         * status(2)： error code ：0为正常值
         *
         * sender context: 用于请求响应消息匹配，接收方始终返回请求数据中的值
         *
         * option：应设置为0？
         */

        /**
         * Table  C-6.1 数据类型定义
         * BOOL             C1 Logical Boolean with values TRUE and FALSE
         * SINT             C2 Signed 8–bit integer value
         * INT              C3 Signed 16–bit integer value
         * DINT             C4 Signed 32–bit integer value
         * LINT             C5 Signed 64–bit integer value
         * USINT            C6 Unsigned 8–bit integer value
         * UINT             C7 Unsigned 16–bit integer value
         * UDINT            C8 Unsigned 32–bit integer value
         * ULINT            C9 Unsigned 64–bit integer value
         * REAL             CA 32–bit floating point value
         * LREAL            CB 64–bit floating point value
         * STIME            CC Synchronous time information
         * DATE             CD Date information
         * TIME_OF_DAY      CE Time of day
         * DATE_AND_TIME    CF Date and time of day
         * STRING           D0 character string (1 byte per character)
         * BYTE             D1 bit string - 8-bits
         * WORD             D2 bit string - 16-bits
         * DWORD            D3 bit string - 32-bits
         */

        public String getErrorMsg(byte b)
        {
            switch (b)
            {
                case 0x01:
                    //                return "The sender issued an invalid or unsupported encapsulation command.";
                    return "命令无效或不支持";
                case 0x02:
                    //                return "Insufficient memory resources in the receiver to handle the command.  This is not an application error.  Instead, it only results if the encapsulation layer cannot obtain memory resources that it needs. ";
                    return "内存资源不足";
                case 0x03:
                    //                return "Poorly formed or incorrect data in the data portion of the encapsulation message. ";
                    return "数据格式不正确";
                case 0x64:
                    //                return "An originator used an invalid session handle when sending an encapsulation message to the target. ";
                    return "会话句柄无效/会话过期";
                case 0x65:
                    //                return "The target received a message of invalid length ";
                    return "目标侧收到数据长度无效";
                case 0x69:
                    //                return "Unsupported encapsulation protocol revision.";
                    return "不支持的协议版本";
                default:
                    return "保留项目!";
            }
        }

        private long getSeq()
        {
            if (seq > long.MaxValue)
            {
                seq = 1;
                return seq++;
            }
            return seq++;
        }

        public void listIdentify()
        {
            byte[] bytes = {
                0x63, 0x00,
                0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00};


        }

        byte[] registerData = {
            0x65, 0x00,
            0x04, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x01, 0x00,
            0x00, 0x00
    };

        byte[] header = {
            0x6f, 0x00,
            0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00
    };


        byte[] comSpecData = {
            0x00, 0x00, 0x00, 0x00,
            0x01, 0x00,
            0x02, 0x00,
            0x00, 0x00,
            0x00, 0x00,
            (byte) 0xB2, 0x00,
            0x18, 0x00,
    };

        byte[] unRegisterData = {
            0x66, 0x00,
            0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
    };
        private byte[] session = new byte[4];


        public OmronCipNet(String ip)
        {
            this.ip = ip;
        }

        public OperateResult connect()
        {
            OperateResult operateResult = new OperateResult();
            IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(ip), port);
            this.socket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.Connect(ipe);
                socket.SendTimeout = 1000;
                socket.ReceiveTimeout = 1000;
                socket.Send(registerData);
                byte[] bytes = new byte[32];
                socket.Receive(bytes);
                log.info("注册会话数据[{}]", bytes.ToString());
                if (bytes[8] != 0)
                {
                    operateResult.success = false;
                    operateResult.msg = getErrorMsg(bytes[8]);
                    return operateResult;
                }
                this.session[0] = bytes[4];
                this.session[1] = bytes[5];
                this.session[2] = bytes[6];
                this.session[3] = bytes[7];
                operateResult.success = true;

            }
            catch (Exception e)
            {
                log.error("[{}]connect error", this.ip, e);
                operateResult.setMsg("error" + e.toString());
                operateResult.setSuccess(false);
            }

            return operateResult;
        }

        public void destoryConnection()
        {
            try
            {
                this.unRegisterData[4] = this.session[0];
                this.unRegisterData[5] = this.session[1];
                this.unRegisterData[6] = this.session[2];
                this.unRegisterData[7] = this.session[3];
                this.session = new byte[4];
                socket.getOutputStream().write(this.unRegisterData);
            }
            catch (Exception e)
            {
                log.error("[{}]注销会话异常", ip, e);
            }

        }

        public OperateResult readMultiTag(List<CipData> tagList)
        {
            OperateResult operateResult = new OperateResult();
            byte[] bytes = buildReadRequestDataForMoreTag(tagList);

            reentrantLock.lock () ;
            try
            {
                this.socket.getOutputStream().write(bytes);
                operateResult = this.sendAndReceive();
                if (operateResult.isSuccess())
                {
                    //// TODO: 2020/4/11 校验ID是否一致
                    byte[] content = operateResult.getContent();
                    if (content[12] != bytes[12] ||
                            content[13] != bytes[13] ||
                            content[14] != bytes[14] ||
                            content[15] != bytes[15] ||
                            content[16] != bytes[16] ||
                            content[17] != bytes[17] ||
                            content[18] != bytes[18] ||
                            content[19] != bytes[19])
                    {
                        operateResult.setSuccess(false);
                        operateResult.setMsg("序列号校验失败");
                        log.warn("发送数据[{},{},{},{}]，响应数据[{},{},{},{}]",
                                bytes[12], bytes[13], bytes[14], bytes[15],
                                bytes[16], bytes[17], bytes[18], bytes[19],
                                content[12], content[13], content[14], content[15],
                                content[16], content[17], content[18], content[19]);
                        return operateResult;
                    }
                    parseReadMultiTagData(tagList, content);
                }
                else
                {
                    log.error("[{}]读取失败,失败信息[{}]", ip, operateResult.getMsg());
                }

            }
            catch (SocketException se)
            {
                log.error("[{}]连接异常，重新连接", ip, se);
                this.destoryConnection();
                this.connect();
                operateResult.setSuccess(false);
                operateResult.setMsg(se.getMessage());

            }
            catch (SocketTimeoutException ste)
            {
                log.error("[{}]连接异常，重新连接", ip, ste);
                this.destoryConnection();
                this.connect();
                operateResult.setSuccess(false);
                operateResult.setMsg(ste.getMessage());
            }
            catch (Exception e)
            {
                log.error("[{}]读取数据异常", ip, e);
                operateResult.setSuccess(false);
                operateResult.setMsg(e.getMessage());
            }
            finally
            {
                reentrantLock.unlock();
            }
            return operateResult;
        }

        /**
         *
         */
        public OperateResult readLongTag(String tagName, int c)
        {
            OperateResult operateResult = new OperateResult();
            byte[] bytes = buildLongReadRequestData(tagName, c);
            reentrantLock.lock () ;
            try
            {
                this.socket.getOutputStream().write(bytes);
                operateResult = this.sendAndReceive();
                if (operateResult.isSuccess())
                {
                    //// TODO: 2020/4/11 校验ID是否一致
                    byte[] content = operateResult.getContent();
                    if (content[12] != bytes[12] ||
                            content[13] != bytes[13] ||
                            content[14] != bytes[14] ||
                            content[15] != bytes[15] ||
                            content[16] != bytes[16] ||
                            content[17] != bytes[17] ||
                            content[18] != bytes[18] ||
                            content[19] != bytes[19])
                    {
                        operateResult.setSuccess(false);
                        operateResult.setMsg("序列号校验失败");
                        log.warn("发送数据[{},{},{},{}]，响应数据[{},{},{},{}]",
                                bytes[12], bytes[13], bytes[14], bytes[15],
                                bytes[16], bytes[17], bytes[18], bytes[19],
                                content[12], content[13], content[14], content[15],
                                content[16], content[17], content[18], content[19]);

                        // 4.21程序一直报该错，log中，响应数据始终未0，0，0，0,故增加重连方法，在此处重新连接目标IP
                        this.destoryConnection();
                        this.connect();
                        return operateResult;
                    }
                    if (content[42] == 0x20)
                    {
                        operateResult.setSuccess(false);
                        operateResult.setMsg(String.format("标签[%s]不存在", tagName));
                        return operateResult;
                    }
                    //                解析数据
                    // TODO: 2020/4/24 获取是否存在
                    return parseReadSingleData(content);
                }
                else
                {
                    log.error("[{}]读取失败,失败信息[{}]", ip, operateResult.getMsg());
                }
            }
            catch (SocketException se)
            {
                log.error("[{}]连接异常，重新连接", ip, se);
                this.destoryConnection();
                this.connect();
                operateResult.setSuccess(false);
                operateResult.setMsg(se.getMessage());

            }
            catch (SocketTimeoutException ste)
            {
                log.error("[{}]连接异常，重新连接", ip, ste);
                this.destoryConnection();
                this.connect();
                operateResult.setSuccess(false);
                operateResult.setMsg(ste.getMessage());
            }
            catch (Exception e)
            {
                log.error("[{}]读取数据异常", ip, e);
                operateResult.setSuccess(false);
                operateResult.setMsg(e.getMessage());
            }
            finally
            {
                reentrantLock.unlock();
            }
            return operateResult;
        }

        /**
         * 读取单个标签
         * @param tagName
         * @return
         */
        public OperateResult readTag(String tagName)
        {
            OperateResult operateResult = new OperateResult();
            byte[] bytes = buildReadRequestData(tagName);
            reentrantLock.lock () ;
            try
            {
                this.socket.getOutputStream().write(bytes);
                operateResult = this.sendAndReceive();
                if (operateResult.isSuccess())
                {
                    //// TODO: 2020/4/11 校验ID是否一致
                    byte[] content = operateResult.getContent();
                    if (content[12] != bytes[12] ||
                            content[13] != bytes[13] ||
                            content[14] != bytes[14] ||
                            content[15] != bytes[15] ||
                            content[16] != bytes[16] ||
                            content[17] != bytes[17] ||
                            content[18] != bytes[18] ||
                            content[19] != bytes[19])
                    {
                        operateResult.setSuccess(false);
                        operateResult.setMsg("序列号校验失败");
                        log.warn("发送数据[{},{},{},{}]，响应数据[{},{},{},{}]",
                                bytes[12], bytes[13], bytes[14], bytes[15],
                                bytes[16], bytes[17], bytes[18], bytes[19],
                                content[12], content[13], content[14], content[15],
                                content[16], content[17], content[18], content[19]);

                        // 4.21程序一直报该错，log中，响应数据始终未0，0，0，0,故增加重连方法，在此处重新连接目标IP
                        this.destoryConnection();
                        this.connect();
                        return operateResult;
                    }
                    if (content[42] == 0x20)
                    {
                        operateResult.setSuccess(false);
                        operateResult.setMsg(String.format("标签[%s]不存在", tagName));
                        return operateResult;
                    }
                    //                解析数据
                    // TODO: 2020/4/24 获取是否存在
                    return parseReadSingleData(content);
                }
                else
                {
                    log.error("[{}]读取失败,失败信息[{}]", ip, operateResult.getMsg());
                }
            }
            catch (SocketException se)
            {
                log.error("[{}]连接异常，重新连接", ip, se);
                this.destoryConnection();
                this.connect();
                operateResult.setSuccess(false);
                operateResult.setMsg(se.getMessage());

            }
            catch (SocketTimeoutException ste)
            {
                log.error("[{}]连接异常，重新连接", ip, ste);
                this.destoryConnection();
                this.connect();
                operateResult.setSuccess(false);
                operateResult.setMsg(ste.getMessage());
            }
            catch (Exception e)
            {
                log.error("[{}]读取数据异常", ip, e);
                operateResult.setSuccess(false);
                operateResult.setMsg(e.getMessage());
            }
            finally
            {
                reentrantLock.unlock();
            }
            return operateResult;
        }

        private OperateResult parseReadSingleData(byte[] content)
        {
            OperateResult operateResult = new OperateResult();

            byte[] len = { content[39], content[38] };
            int dataLen = ConvertUtils.byte2Int(len);
            byte[] responseData = new byte[dataLen];
            System.arraycopy(content, 40, responseData, 0, dataLen);
            if (responseData[2] == 4)
            {
                operateResult.setSuccess(false);
                operateResult.setMsg("标签不存在");
                return operateResult;
            }
            if (responseData[2] == 17)
            {
                operateResult.setSuccess(false);
                operateResult.setMsg("数据太大");
                return operateResult;
            }
            log.info("响应数据为[{}]", Arrays.toString(responseData));
            if (responseData[4] == (byte)0xa0 && responseData[5] == 0x02)
            {
                byte[] validData = new byte[dataLen - 8];
                System.arraycopy(responseData, 8, validData, 0, dataLen - 8);
                operateResult.setSuccess(true);
                operateResult.setContent(validData);
            }
            else if (responseData[4] == (byte)0xd0 && responseData[5] == 0x00)
            {
                // 根据，6，7位值，计算字符串长度
                byte[] strLen = { responseData[7], responseData[6] };
                int dLen = ConvertUtils.byte2Int(strLen);
                byte[] validData = new byte[dLen];
                System.arraycopy(responseData, 8, validData, 0, dLen);
                operateResult.setSuccess(true);
                operateResult.setContent(validData);
            }
            else
            {
                byte[] validData = new byte[dataLen - 6];
                System.arraycopy(responseData, 6, validData, 0, dataLen - 6);
                operateResult.setSuccess(true);
                operateResult.setContent(validData);
            }
            //当String为空时，返回返回一定的数据
            return operateResult;
        }

        private void parseReadMultiTagData(List<CipData> tagList, byte[] content)
        {
            //获取内容长度
            byte[] len = { content[39], content[38] };
            int dataLen = ConvertUtils.byte2Int(len);

            byte[] response = new byte[dataLen];
            System.arraycopy(content, 40, response, 0, dataLen);
            // TODO: 2020/4/13  针对多项目同时读取数据，标签不存在时，触发此逻辑
            if (response[2] == 0x20)
            {
                log.info("[{}]Invalid parameter", ip, Arrays.toString(response));
                return;
            }
            if (response[2] == 0x11)
            {
                log.info("[{}]too large data", ip, Arrays.toString(response));
                return;
            }
            byte[] validData = new byte[dataLen - 6];
            System.arraycopy(content, 46, validData, 0, dataLen - 6);
            if (log.isDebugEnabled())
            {
                log.debug("[{}]响应数据[{}]", ip, Arrays.toString(validData));
            }


            int temp = 0;
            for (int i = 0; i < tagList.size(); i++)
            {
                byte[] itemLen = { validData[1 + temp], validData[0 + temp] };
                int itemDataLen = ConvertUtils.byte2Int(itemLen);
                // TODO: 2020/4/13 此处可能有bug，可能有两个字节为多余字节
                // 待确认，测试点位响应数据
                byte[] bytes = { };
                if (validData[2 + temp] == (byte)0xD0)
                {
                    byte[] strLen = { validData[5 + temp], validData[4 + temp] };
                    bytes = new byte[ConvertUtils.byte2Int(strLen)];
                    System.arraycopy(validData, temp + 6, bytes, 0, bytes.length);


                }
                else if (validData[2 + temp] == (byte)0xC1)
                {
                    // bool
                    bytes = new byte[2];
                    bytes[0] = validData[temp + 4];
                    bytes[1] = validData[temp + 5];
                }
                else if (validData[2 + temp] == (byte)0xC3)
                {
                    // int
                    bytes = new byte[2];
                    bytes[0] = validData[temp + 4];
                    bytes[1] = validData[temp + 5];
                }
                else if (validData[2 + temp] == (byte)0xC4)
                {
                    // dint
                    bytes = new byte[4];
                    bytes[0] = validData[temp + 4];
                    bytes[1] = validData[temp + 5];
                    bytes[2] = validData[temp + 6];
                    bytes[3] = validData[temp + 7];

                }
                else if (validData[2 + temp] == (byte)0xa0 && validData[3 + temp] == 0x02)
                {
                    bytes = new byte[itemDataLen - 4];
                    System.arraycopy(validData, temp + 4 + 2, bytes, 0, bytes.length);

                }
                else
                {

                }

                temp += 2 + itemDataLen;
                tagList.get(i).setData(bytes);
            }

        }

        /**
         * 读取多个标签数据，返回对应的字节数组
         *
         * @param tagList
         * @return
         */
        private byte[] buildReadRequestDataForMoreTag(List<CipData> tagList)
        {

            // TODO: 2020/4/11 协议解析结果
            byte[] cipMessage = {

                0x50, 0x02,
                0x20, 0x6d, 0x24, 0x00,

                0x00, 0x00,
                (byte) 0xff, 0x3f,
                0x00, 0x00,
                (byte) 0x82, (byte) 0xc1,
                0x01, 0x00,
//                0x10, 0x00,
//                (byte) 0x91,
//                0x0e,
//                0x52, 0x56,
//                0x5f, 0x45,
//                0x51, 0x50,
//                0x45, 0x56,
//                0x45, 0x4e,
//                0x54, 0x5f,
//                0x4c, 0x32,
        };
            /**
             * 头数据，项目数量，项目1整体数据长度，扩展代码，项目1实际长度，项目1内容
             *                  项目2整体数据长度，扩展代码，项目2实际长度，项目2内容
             */

            int sum = 16;
            for (int i = 0; i < tagList.size(); i++)
            {

                int length = tagList.get(i).getTagName().length();
                sum = sum + length + 4;
                if (length % 2 == 1)
                {
                    sum++;
                }
            }

            byte[] body = new byte[sum];
            System.arraycopy(cipMessage, 0, body, 0, cipMessage.length);

            //        设置标签个数
            byte[] tagLength = ConvertUtils.int2ByteArray(tagList.size());
            body[14] = tagLength[1];
            body[15] = tagLength[0];

            int temp = 0;
            for (int i = 0; i < tagList.size(); i++)
            {
                String tagName = tagList.get(i).getTagName();
                int length = tagName.length();
                byte[] tagData = { };
                if (length % 2 == 1)
                {
                    tagData = new byte[length + 1 + 4];

                    tagData[length + 4] = 0;
                }
                else
                {
                    tagData = new byte[length + 4];
                }
                System.arraycopy(tagName.getBytes(), 0, tagData, 4, length);
                byte[] tagLen = ConvertUtils.int2ByteArray(length);
                byte[] dataLen = ConvertUtils.int2ByteArray(tagData.length - 2);
                //设置标签长度
                tagData[0] = dataLen[1];
                tagData[1] = dataLen[0];
                tagData[2] = (byte)0x91;
                tagData[3] = tagLen[1];

                //合并数据
                System.arraycopy(tagData, 0, body, 16 + temp, tagData.length);
                temp += tagData.length;

            }

            byte[] msgLen = ConvertUtils.int2ByteArray(body.length);
            comSpecData[14] = msgLen[1];
            comSpecData[15] = msgLen[0];

            byte[] sumLen = ConvertUtils.int2ByteArray(body.length + 16);
            header[2] = sumLen[1];
            header[3] = sumLen[0];

            header[4] = this.session[0];
            header[5] = this.session[1];
            header[6] = this.session[2];
            header[7] = this.session[3];
            long seq = getSeq();
            byte[] bytes = ConvertUtils.longToByteArray(seq);
            header[12] = bytes[0];
            header[13] = bytes[1];
            header[14] = bytes[2];
            header[15] = bytes[3];
            header[16] = bytes[4];
            header[17] = bytes[5];
            header[18] = bytes[6];
            header[19] = bytes[7];

            byte[] data = new byte[24 + 16 + body.length];

            System.arraycopy(header, 0, data, 0, 24);
            System.arraycopy(comSpecData, 0, data, 24, 16);
            System.arraycopy(body, 0, data, 40, body.length);

            return data;
        }

        public OperateResult writeTagForFloat(String tagName, byte[] data)
        {
            OperateResult operateResult = new OperateResult();
            byte[] bytes = buildWriteRequestDataForFloat(tagName, data);
            reentrantLock.lock () ;
            try
            {
                this.socket.getOutputStream().write(bytes);
                operateResult = sendAndReceive();
                if (operateResult.isSuccess())
                {
                    if (operateResult.getContent().length >= 44)
                    {
                        if (operateResult.getContent()[42] == 0x20)
                        {
                            operateResult.setSuccess(false);
                            operateResult.setMsg("参数有误");

                        }
                    }
                    else
                    {
                        operateResult.setSuccess(false);
                        operateResult.setMsg("响应数据长度不符合预期");
                    }
                }
            }
            catch (Exception e)
            {

            }
            finally
            {
                reentrantLock.unlock();
            }
            return operateResult;
        }
        private byte[] buildWriteRequestDataForFloat(String tagName, byte[] data)
        {
            byte[] cipMessage = {
                0x52, 0x02,
                0x20, 0x06, 0x24, 0x01,
                0x06, (byte) 0x9c,
                (byte) 0xaa, 0x01,

                0x4d,
                0x09,
                (byte) 0x91,
                0x0f,


                (byte) 0xCA, 0x00,
                0x01, 0x00,
                0x01,
                0x00,
                0x01, 0x00
        };


            int tagLength = tagName.length();
            if (tagName.length() % 2 == 1)
            {
                tagLength++;
            }

            byte[] content = new byte[14 + tagLength + data.length + 8];

            System.arraycopy(cipMessage, 0, content, 0, 14);
            System.arraycopy(tagName.getBytes(), 0, content, 14, tagName.length());
            System.arraycopy(cipMessage, 14, content, 14 + tagLength, 4);
            System.arraycopy(data, 0, content, 18 + tagLength, data.length);
            System.arraycopy(cipMessage, 18, content, 18 + tagLength + data.length, 4);

            byte[] tagLen = ConvertUtils.int2ByteArray(tagName.length());
            content[13] = tagLen[1];

            byte[] pathSize = ConvertUtils.int2ByteArray((tagName.length() + 1) / 2 + 1);
            content[11] = pathSize[1];

            byte[] msgLen = ConvertUtils.int2ByteArray(content.length - 14);
            content[8] = msgLen[1];
            content[9] = msgLen[0];

            byte[] requestData = new byte[24 + 16 + content.length];

            System.arraycopy(header, 0, requestData, 0, 24);
            System.arraycopy(comSpecData, 0, requestData, 24, 16);
            System.arraycopy(content, 0, requestData, 24 + 16, content.length);

            byte[] conLen = ConvertUtils.int2ByteArray(content.length);
            requestData[38] = conLen[1];
            requestData[39] = conLen[0];

            byte[] bodyLen = ConvertUtils.int2ByteArray(comSpecData.length + content.length);
            requestData[2] = bodyLen[1];
            requestData[3] = bodyLen[0];

            System.arraycopy(this.session, 0, requestData, 4, 4);

            long seq = getSeq();
            byte[] sender = ConvertUtils.longToByteArray(seq);

            System.arraycopy(sender, 0, requestData, 12, 8);

            return requestData;
        }


        /**
         * 读取单个标签，返回对应的字节数组
         *
         * @param tagName
         * @return
         */
        private byte[] buildLongReadRequestData(String tagName, int c)
        {

            byte[] header = {
                0x6f, 0x00,
                0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00
        };


            byte[] comSpecData = {
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00,
                0x02, 0x00,
                0x00, 0x00,
                0x00, 0x00,
                (byte) 0xB2, 0x00,
                0x18, 0x00,
        };


            // TODO: 2020/4/11 协议解析结果
            byte[] cipMessage = {

                0x52, 0x02,
                0x20, 0x06, 0x24, 0x01,

                0x06, (byte) 0x9c,
                0x0A, 0x00,
                0x4C,
                0x03,
                (byte) 0x91,
                0x04,

                0x01, 0x00,
                0x01, 0x00, 0x01, 0x00
        };
            /**
             * 头数据，项目数量，项目1整体数据长度，扩展代码，项目1实际长度，项目1内容
             *                  项目2整体数据长度，扩展代码，项目2实际长度，项目2内容
             */
            byte[] body = { };
            byte[] tagByte = { };
            if (tagName.length() % 2 == 1)
            {

                body = new byte[28 + tagName.length() + 1];
                tagByte = new byte[tagName.length() + 1];
                System.arraycopy(tagName.getBytes(), 0, tagByte, 0, tagName.length());
            }
            else
            {
                body = new byte[28 + tagName.length()];
                tagByte = tagName.getBytes();
            }

            System.arraycopy(cipMessage, 0, body, 0, 14);

            System.arraycopy(tagByte, 0, body, 14, tagByte.length);

            byte[] simpleDatSegment1 = { (byte)0x80, 0x03, 0x00, 0x00, 0x00, 0x00, (byte)0xea, 0x01 };
            byte[] simpleDatSegment2 = { (byte)0x80, 0x03, (byte)0xea, 0x01, 0x00, 0x00, (byte)0x24, 0x00 };
            if (c == 1)
            {
                System.arraycopy(simpleDatSegment1, 0, body, 14 + tagByte.length, 8);
            }
            else
            {
                System.arraycopy(simpleDatSegment2, 0, body, 14 + tagByte.length, 8);
            }
            System.arraycopy(cipMessage, 14, body, 14 + tagByte.length + 8, 6);


            //标签长度
            byte[] tagLen = ConvertUtils.int2ByteArray(tagName.length());
            body[13] = tagLen[1];
            byte[] nodeLen = ConvertUtils.int2ByteArray((tagName.length() + 8 + 1) / 2 + 1);
            body[11] = nodeLen[1];
            byte[] cmdLen = ConvertUtils.int2ByteArray(tagByte.length + 8 + 6);
            body[8] = cmdLen[1];
            body[9] = cmdLen[0];


            byte[] msgLen = ConvertUtils.int2ByteArray(body.length);
            comSpecData[14] = msgLen[1];
            comSpecData[15] = msgLen[0];

            byte[] sumLen = ConvertUtils.int2ByteArray(body.length + 16);
            header[2] = sumLen[1];
            header[3] = sumLen[0];

            header[4] = this.session[0];
            header[5] = this.session[1];
            header[6] = this.session[2];
            header[7] = this.session[3];
            long seq = getSeq();
            byte[] bytes = ConvertUtils.longToByteArray(seq);
            header[12] = bytes[0];
            header[13] = bytes[1];
            header[14] = bytes[2];
            header[15] = bytes[3];
            header[16] = bytes[4];
            header[17] = bytes[5];
            header[18] = bytes[6];
            header[19] = bytes[7];

            byte[] data = new byte[24 + 16 + body.length];

            System.arraycopy(header, 0, data, 0, 24);
            System.arraycopy(comSpecData, 0, data, 24, 16);
            System.arraycopy(body, 0, data, 40, body.length);

            return data;
        }

        private byte[] buildReadRequestData(String tagName)
        {

            byte[] header = {
                0x6f, 0x00,
                0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00
        };


            byte[] comSpecData = {
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00,
                0x02, 0x00,
                0x00, 0x00,
                0x00, 0x00,
                (byte) 0xB2, 0x00,
                0x18, 0x00,
        };


            // TODO: 2020/4/11 协议解析结果
            byte[] cipMessage = {

                0x52, 0x02,
                0x20, 0x06, 0x24, 0x01,

                0x06, (byte) 0x9c,
                0x0A, 0x00,
                0x4C,
                0x03,
                (byte) 0x91,
                0x04,

                0x01, 0x00,
                0x01, 0x00, 0x01, 0x00
        };
            /**
             * 头数据，项目数量，项目1整体数据长度，扩展代码，项目1实际长度，项目1内容
             *                  项目2整体数据长度，扩展代码，项目2实际长度，项目2内容
             */
            byte[] body = { };
            byte[] tagByte = { };
            if (tagName.length() % 2 == 1)
            {

                body = new byte[20 + tagName.length() + 1];
                tagByte = new byte[tagName.length() + 1];
                System.arraycopy(tagName.getBytes(), 0, tagByte, 0, tagName.length());
            }
            else
            {
                body = new byte[20 + tagName.length()];
                tagByte = tagName.getBytes();
            }

            System.arraycopy(cipMessage, 0, body, 0, 14);

            System.arraycopy(tagByte, 0, body, 14, tagByte.length);

            System.arraycopy(cipMessage, 14, body, 14 + tagByte.length, 6);


            //标签长度
            byte[] tagLen = ConvertUtils.int2ByteArray(tagName.length());
            body[13] = tagLen[1];
            //节点长度
            byte[] nodeLen = ConvertUtils.int2ByteArray((tagName.length() + 1) / 2 + 1);
            body[11] = nodeLen[1];
            //cip 指令长度
            byte[] cmdLen = ConvertUtils.int2ByteArray(tagByte.length + 6);
            body[8] = cmdLen[1];
            body[9] = cmdLen[0];


            byte[] msgLen = ConvertUtils.int2ByteArray(body.length);
            comSpecData[14] = msgLen[1];
            comSpecData[15] = msgLen[0];

            byte[] sumLen = ConvertUtils.int2ByteArray(body.length + 16);
            header[2] = sumLen[1];
            header[3] = sumLen[0];

            header[4] = this.session[0];
            header[5] = this.session[1];
            header[6] = this.session[2];
            header[7] = this.session[3];
            long seq = getSeq();
            byte[] bytes = ConvertUtils.longToByteArray(seq);
            header[12] = bytes[0];
            header[13] = bytes[1];
            header[14] = bytes[2];
            header[15] = bytes[3];
            header[16] = bytes[4];
            header[17] = bytes[5];
            header[18] = bytes[6];
            header[19] = bytes[7];

            byte[] data = new byte[24 + 16 + body.length];

            System.arraycopy(header, 0, data, 0, 24);
            System.arraycopy(comSpecData, 0, data, 24, 16);
            System.arraycopy(body, 0, data, 40, body.length);

            return data;
        }

        private OperateResult sendAndReceive() throws Exception
        {
            OperateResult operateResult = new OperateResult();
        byte[] header = new byte[24];
        socket.getInputStream().read(header);
        byte[] lenBytes = { header[3], header[2] };
        int len = ConvertUtils.byte2Int(lenBytes);
        byte[] body = new byte[len];
        socket.getInputStream().read(body);
        byte[] result = new byte[24 + len];
        System.arraycopy(header, 0, result, 0, 24);
        System.arraycopy(body, 0, result, 24, len);
        operateResult.setSuccess(true);
        operateResult.setContent(result);

        return operateResult;
    }
}
}
