Unknown server lines: See at tyrbot core/tyrbot.py function 
 def public_channel_message_ext_msg_handling(self, packet: server_packets.PublicChannelMessage):
        msg = packet.message
        if msg.startswith("~&") and msg.endswith("~"):
            try:
                msg = msg[2:-1].encode("utf-8")
                category_id = self.mmdb_parser.read_base_85(msg[0:5])
                instance_id = self.mmdb_parser.read_base_85(msg[5: 10])
                template = self.mmdb_parser.get_message_string(category_id, instance_id)
                params = self.mmdb_parser.parse_params(msg[10:])
                packet.extended_message = ExtendedMessage(category_id, instance_id, template, params)
            except Exception as e:
                self.logger.error("Error handling extended message for packet: " + str(packet), e)

        return packet

and the mmdb.txt for the dictionary of the server lines.

This instead of the RawFormatted method in Missioncontroller (probably not the right place for it anyway) should yield better results.