using System;
using NUnit.Framework;
using SharpPcap.LibPcap;
using PacketDotNet;
using System.Collections.Generic;
using PacketDotNet.Lsa;
using SharpPcap;
using PacketDotNet.Utils;

namespace Test.PacketType
{
    [TestFixture]
    public class OspfV2PacketTest
    {
        private const int OSPF_HELLO_INDEX = 0;
        private const int OSPF_DBD_INDEX = 9;
        private const int OSPF_DBD_LSA_INDEX = 11;
        private const int OSPF_LSR_INDEX = 17;
        private const int OSPF_LSU_INDEX = 18;
        private const int OSPF_LSA_INDEX = 23;
        private const int OSPF_LSU_LSA_INDEX = 31;

        OspfV2Packet helloPacket;
        OspfV2Packet ddPacket;
        OspfV2Packet ddLSAPacket;
        OspfV2Packet lsrPacket;
        OspfV2Packet lsuPacket;
        OspfV2Packet lsaPacket;

        OspfV2LinkStateUpdatePacket lsaHolder;

        bool packetsLoaded = false;

        [SetUp]
        public void Init()
        {
            if (packetsLoaded)
                return;

            RawCapture raw;
            int packetIndex = 0;
            var dev = new CaptureFileReaderDevice("../../CaptureFiles/ospfv2.pcap");
            dev.Open();


            while ((raw = dev.GetNextPacket()) != null)
            {
                OspfV2Packet p = Packet.ParsePacket(raw.LinkLayerType, raw.Data).Extract<OspfV2Packet>();

                switch (packetIndex)
                {
                    case OSPF_HELLO_INDEX:
                        helloPacket = p;
                        break;
                    case OSPF_DBD_INDEX:
                        ddPacket = p;
                        break;
                    case OSPF_DBD_LSA_INDEX:
                        ddLSAPacket = p;
                        break;
                    case OSPF_LSU_INDEX:
                        lsuPacket = p;
                        break;
                    case OSPF_LSA_INDEX:
                        lsaPacket = p;
                        break;
                    case OSPF_LSR_INDEX:
                        lsrPacket = p;
                        break;
                    case OSPF_LSU_LSA_INDEX:
                        lsaHolder = (OspfV2LinkStateUpdatePacket)p;
                        break;
                    default: /* do nothing */break;
                }

                packetIndex++;
            }
            dev.Close();

            packetsLoaded = true;
        }

        [Test]
        public void TestHelloPacket()
        {
            OspfV2HelloPacket hp = null;
            Assert.IsNotNull(helloPacket);
            Assert.AreEqual(helloPacket is OspfV2HelloPacket, true);
            hp = (OspfV2HelloPacket)helloPacket;
            Assert.AreEqual(OspfVersion.OspfV2, helloPacket.Version);
            Assert.AreEqual(OspfPacketType.Hello, helloPacket.Type);
            Assert.AreEqual(0x273b, helloPacket.Checksum);
            Assert.AreEqual(System.Net.IPAddress.Parse("255.255.255.0"), hp.NetworkMask);
            Assert.AreEqual(0x02, hp.HelloOptions);
            Assert.AreEqual(0, hp.NeighborIds.Count);
        }

        [Test]
        public void TestDBDPacket()
        {
            OspfV2DatabaseDescriptorPacket dp = null;
            Assert.IsNotNull(ddPacket);
            Assert.AreEqual(OspfVersion.OspfV2, ddPacket.Version);
            Assert.AreEqual(OspfPacketType.DatabaseDescription, ddPacket.Type);
            Assert.AreEqual(ddPacket is OspfV2DatabaseDescriptorPacket, true);
            Assert.AreEqual(0xa052, ddPacket.Checksum);
            dp = (OspfV2DatabaseDescriptorPacket)ddPacket;
            Assert.AreEqual(1098361214, dp.DDSequence);
            Assert.AreEqual(0, dp.Headers.Count);
        }

        [Test]
        public void TestLSUPacket()
        {
            OspfV2LinkStateUpdatePacket lp = null;
            Assert.IsNotNull(lsuPacket);
            Assert.AreEqual(OspfVersion.OspfV2, lsuPacket.Version);
            Assert.AreEqual(OspfPacketType.LinkStateUpdate, lsuPacket.Type);
            Assert.AreEqual(0x961f, lsuPacket.Checksum);
            Assert.AreEqual(lsuPacket is OspfV2LinkStateUpdatePacket, true);
            lp = (OspfV2LinkStateUpdatePacket)lsuPacket;
            Assert.AreEqual(1, lp.LsaNumber);
            List<LinkStateAdvertisement> l = lp.Updates;
            Assert.AreEqual(1, l.Count);
            Assert.AreEqual(typeof(RouterLinksAdvertisement), l[0].GetType());
            Console.WriteLine(l[0]);
        }

        [Test]
        public void TestLSRPacket()
        {
            OspfV2LinkStateRequestPacket lp = null;
            Assert.IsNotNull(lsrPacket);
            Assert.AreEqual(OspfVersion.OspfV2, lsrPacket.Version);
            Assert.AreEqual(OspfPacketType.LinkStateRequest, lsrPacket.Type);
            Assert.AreEqual(0x7595, lsrPacket.Checksum);
            Assert.AreEqual(lsrPacket is OspfV2LinkStateRequestPacket, true);
            lp = (OspfV2LinkStateRequestPacket)lsrPacket;
            Assert.AreEqual(7, lp.Requests.Count);
        }

        [Test]
        public void TestLSAPacket()
        {
            Assert.IsNotNull(lsaPacket);
            Assert.AreEqual(OspfVersion.OspfV2, lsaPacket.Version);
            Assert.AreEqual(OspfPacketType.LinkStateAcknowledgment, lsaPacket.Type);
            Assert.AreEqual(0xe95e, lsaPacket.Checksum);
            Assert.AreEqual(284, lsaPacket.PacketLength);
            Assert.AreEqual(System.Net.IPAddress.Parse("0.0.0.1"), lsaPacket.AreaId);
            OspfV2LinkStateAcknowledgmentPacket ack = (OspfV2LinkStateAcknowledgmentPacket)lsaPacket;
            Assert.AreEqual(13, ack.Acknowledgments.Count);

            LinkStateAdvertisement l = ack.Acknowledgments[0];
            Assert.AreEqual(l.Age, 2);
            Assert.AreEqual(l.Options, 2);
            Assert.AreEqual(l.Type, LinkStateAdvertisementType.Router);
            Assert.AreEqual(l.Id, System.Net.IPAddress.Parse("192.168.170.3"));
            Assert.AreEqual(l.AdvertisingRouter, System.Net.IPAddress.Parse("192.168.170.3"));
            Assert.AreEqual(l.SequenceNumber, 0x80000001);
            Assert.AreEqual(l.Checksum, 0x3a9c);
            Assert.AreEqual(l.Length, 48);

            l = ack.Acknowledgments[1];
            Assert.AreEqual(l.Age, 3);
            Assert.AreEqual(l.Options, 2);
            Assert.AreEqual(l.Type, LinkStateAdvertisementType.ASExternal);
            Assert.AreEqual(l.Id, System.Net.IPAddress.Parse("80.212.16.0"));
            Assert.AreEqual(l.AdvertisingRouter, System.Net.IPAddress.Parse("192.168.170.2")) ;
            Assert.AreEqual(l.SequenceNumber, 0x80000001);
            Assert.AreEqual(l.Checksum, 0x2a49);
            Assert.AreEqual(l.Length, 36);

            l = ack.Acknowledgments[2];
            Assert.AreEqual(l.Age, 3);
            Assert.AreEqual(l.Options, 2);
            Assert.AreEqual(l.Type, LinkStateAdvertisementType.ASExternal);
            Assert.AreEqual(l.Id, System.Net.IPAddress.Parse("148.121.171.0"));
            Assert.AreEqual(l.AdvertisingRouter, System.Net.IPAddress.Parse("192.168.170.2"));
            Assert.AreEqual(l.SequenceNumber, 0x80000001);
            Assert.AreEqual(l.Checksum, 0x34a5);
            Assert.AreEqual(l.Length, 36);

            l = ack.Acknowledgments[3];
            Assert.AreEqual(l.Age, 3);
            Assert.AreEqual(l.Options, 2);
            Assert.AreEqual(l.Type, LinkStateAdvertisementType.ASExternal);
            Assert.AreEqual(l.Id, System.Net.IPAddress.Parse("192.130.120.0"));
            Assert.AreEqual(l.AdvertisingRouter, System.Net.IPAddress.Parse("192.168.170.2"));
            Assert.AreEqual(l.SequenceNumber, 0x80000001);
            Assert.AreEqual(l.Checksum, 0xd319);
            Assert.AreEqual(l.Length, 36);

            l = ack.Acknowledgments[4];
            Assert.AreEqual(l.Age, 3);
            Assert.AreEqual(l.Options, 2);
            Assert.AreEqual(l.Type, LinkStateAdvertisementType.ASExternal);
            Assert.AreEqual(l.Id, System.Net.IPAddress.Parse("192.168.0.0"));
            Assert.AreEqual(l.AdvertisingRouter, System.Net.IPAddress.Parse("192.168.170.2"));
            Assert.AreEqual(l.SequenceNumber, 0x80000001);
            Assert.AreEqual(l.Checksum, 0x3708);
            Assert.AreEqual(l.Length, 36);

            l = ack.Acknowledgments[5];
            Assert.AreEqual(l.Age, 3);
            Assert.AreEqual(l.Options, 2);
            Assert.AreEqual(l.Type, LinkStateAdvertisementType.ASExternal);
            Assert.AreEqual(l.Id, System.Net.IPAddress.Parse("192.168.1.0"));
            Assert.AreEqual(l.AdvertisingRouter, System.Net.IPAddress.Parse("192.168.170.2"));
            Assert.AreEqual(l.SequenceNumber, 0x80000001);
            Assert.AreEqual(l.Checksum, 0x2c12);
            Assert.AreEqual(l.Length, 36);

            l = ack.Acknowledgments[6];
            Assert.AreEqual(l.Age, 3);
            Assert.AreEqual(l.Options, 2);
            Assert.AreEqual(l.Type, LinkStateAdvertisementType.ASExternal);
            Assert.AreEqual(l.Id, System.Net.IPAddress.Parse("192.168.172.0"));
            Assert.AreEqual(l.AdvertisingRouter, System.Net.IPAddress.Parse("192.168.170.2"));
            Assert.AreEqual(l.SequenceNumber, 0x80000001);
            Assert.AreEqual(l.Checksum, 0x3341);
            Assert.AreEqual(l.Length, 36);

            l = ack.Acknowledgments[7];
            Assert.AreEqual(l.Age, 1);
            Assert.AreEqual(l.Options, 2);
            Assert.AreEqual(l.Type, LinkStateAdvertisementType.ASExternal);
            Assert.AreEqual(l.Id, System.Net.IPAddress.Parse("148.121.171.0"));
            Assert.AreEqual(l.AdvertisingRouter, System.Net.IPAddress.Parse("192.168.170.3"));
            Assert.AreEqual(l.SequenceNumber, 0x80000001);
            Assert.AreEqual(l.Checksum, 0x2eaa);
            Assert.AreEqual(l.Length, 36);

            l = ack.Acknowledgments[8];
            Assert.AreEqual(l.Age, 1);
            Assert.AreEqual(l.Options, 2);
            Assert.AreEqual(l.Type, LinkStateAdvertisementType.ASExternal);
            Assert.AreEqual(l.Id, System.Net.IPAddress.Parse("192.130.120.0"));
            Assert.AreEqual(l.AdvertisingRouter, System.Net.IPAddress.Parse("192.168.170.3"));
            Assert.AreEqual(l.SequenceNumber, 0x80000001);
            Assert.AreEqual(l.Checksum, 0xcd1e);
            Assert.AreEqual(l.Length, 36);

            l = ack.Acknowledgments[9];
            Assert.AreEqual(l.Age, 1);
            Assert.AreEqual(l.Options, 2);
            Assert.AreEqual(l.Type, LinkStateAdvertisementType.ASExternal);
            Assert.AreEqual(l.Id, System.Net.IPAddress.Parse("192.168.0.0"));
            Assert.AreEqual(l.AdvertisingRouter, System.Net.IPAddress.Parse("192.168.170.3"));
            Assert.AreEqual(l.SequenceNumber, 0x80000001);
            Assert.AreEqual(l.Checksum, 0x310d);
            Assert.AreEqual(l.Length, 36);

            l = ack.Acknowledgments[10];
            Assert.AreEqual(l.Age, 1);
            Assert.AreEqual(l.Options, 2);
            Assert.AreEqual(l.Type, LinkStateAdvertisementType.ASExternal);
            Assert.AreEqual(l.Id, System.Net.IPAddress.Parse("192.168.1.0"));
            Assert.AreEqual(l.AdvertisingRouter, System.Net.IPAddress.Parse("192.168.170.3"));
            Assert.AreEqual(l.SequenceNumber, 0x80000001);
            Assert.AreEqual(l.Checksum, 0x2617);
            Assert.AreEqual(l.Length, 36);

            l = ack.Acknowledgments[11];
            Assert.AreEqual(l.Age, 1);
            Assert.AreEqual(l.Options, 2);
            Assert.AreEqual(l.Type, LinkStateAdvertisementType.ASExternal);
            Assert.AreEqual(l.Id, System.Net.IPAddress.Parse("192.168.172.0"));
            Assert.AreEqual(l.AdvertisingRouter, System.Net.IPAddress.Parse("192.168.170.3"));
            Assert.AreEqual(l.SequenceNumber, 0x80000001);
            Assert.AreEqual(l.Checksum, 0x2d46);
            Assert.AreEqual(l.Length, 36);

            l = ack.Acknowledgments[12];
            Assert.AreEqual(l.Age, 1);
            Assert.AreEqual(l.Options, 2);
            Assert.AreEqual(l.Type, LinkStateAdvertisementType.ASExternal);
            Assert.AreEqual(l.Id, System.Net.IPAddress.Parse("80.212.16.0"));
            Assert.AreEqual(l.AdvertisingRouter, System.Net.IPAddress.Parse("192.168.170.3"));
            Assert.AreEqual(l.SequenceNumber, 0x80000001);
            Assert.AreEqual(l.Checksum, 0x244e);
            Assert.AreEqual(l.Length, 36);
        }

        [Test]
        public void TestDDWithLSA()
        {
            OspfV2DatabaseDescriptorPacket dp = null;
            Assert.IsNotNull(ddLSAPacket);
            Assert.AreEqual(OspfVersion.OspfV2, ddLSAPacket.Version);
            Assert.AreEqual(OspfPacketType.DatabaseDescription, ddLSAPacket.Type);
            Assert.AreEqual(0xf067, ddLSAPacket.Checksum);
            dp = (OspfV2DatabaseDescriptorPacket)ddLSAPacket;
            Assert.AreEqual(1098361214, dp.DDSequence);
            Assert.AreEqual(172, ddLSAPacket.PacketLength);

            List<LinkStateAdvertisement> lsas = dp.Headers;
            Assert.AreEqual(7, lsas.Count);
            LinkStateAdvertisement l = lsas[0];
            Console.WriteLine(l);
            Assert.AreEqual(l.Checksum, 0x3a9c);
            Assert.AreEqual(l.AdvertisingRouter, System.Net.IPAddress.Parse("192.168.170.3"));
            Assert.AreEqual(l.Length, 48);
            Assert.AreEqual(l.Id, System.Net.IPAddress.Parse("192.168.170.3"));
            Assert.AreEqual(l.Age, 1);
            Assert.AreEqual(l.SequenceNumber, 0x80000001);
            Assert.AreEqual(l.Type, LinkStateAdvertisementType.Router);
            Assert.AreEqual(l.Options, 0x02);

            l = lsas[1];
            Assert.AreEqual(l.Checksum, 0x2a49);
            Assert.AreEqual(l.AdvertisingRouter, System.Net.IPAddress.Parse("192.168.170.2"));
            Assert.AreEqual(l.Length, 36);
            Assert.AreEqual(l.Id, System.Net.IPAddress.Parse("80.212.16.0"));
            Assert.AreEqual(l.Age, 2);
            Assert.AreEqual(l.SequenceNumber, 0x80000001);
            Assert.AreEqual(l.Type, LinkStateAdvertisementType.ASExternal);
            Assert.AreEqual(l.Options, 0x02);

            l = lsas[2];
            Assert.AreEqual(l.Checksum, 0x34a5);
            Assert.AreEqual(l.AdvertisingRouter, System.Net.IPAddress.Parse("192.168.170.2"));
            Assert.AreEqual(l.Length, 36);
            Assert.AreEqual(l.Id, System.Net.IPAddress.Parse("148.121.171.0"));
            Assert.AreEqual(l.Age, 2);
            Assert.AreEqual(l.SequenceNumber, 0x80000001);
            Assert.AreEqual(l.Type, LinkStateAdvertisementType.ASExternal);
            Assert.AreEqual(l.Options, 0x02);

            l = lsas[3];
            Assert.AreEqual(l.Checksum, 0xd319);
            Assert.AreEqual(l.AdvertisingRouter, System.Net.IPAddress.Parse("192.168.170.2"));
            Assert.AreEqual(l.Length, 36);
            Assert.AreEqual(l.Id, System.Net.IPAddress.Parse("192.130.120.0"));
            Assert.AreEqual(l.Age, 2);
            Assert.AreEqual(l.SequenceNumber, 0x80000001);
            Assert.AreEqual(l.Type, LinkStateAdvertisementType.ASExternal);
            Assert.AreEqual(l.Options, 0x02);

            l = lsas[4];
            Assert.AreEqual(l.Checksum, 0x3708);
            Assert.AreEqual(l.AdvertisingRouter, System.Net.IPAddress.Parse("192.168.170.2"));
            Assert.AreEqual(l.Length, 36);
            Assert.AreEqual(l.Id, System.Net.IPAddress.Parse("192.168.0.0"));
            Assert.AreEqual(l.Age, 2);
            Assert.AreEqual(l.SequenceNumber, 0x80000001);
            Assert.AreEqual(l.Type, LinkStateAdvertisementType.ASExternal);
            Assert.AreEqual(l.Options, 0x02);

            l = lsas[5];
            Assert.AreEqual(l.Checksum, 0x2c12);
            Assert.AreEqual(l.AdvertisingRouter, System.Net.IPAddress.Parse("192.168.170.2"));
            Assert.AreEqual(l.Length, 36);
            Assert.AreEqual(l.Id, System.Net.IPAddress.Parse("192.168.1.0"));
            Assert.AreEqual(l.Age, 2);
            Assert.AreEqual(l.SequenceNumber, 0x80000001);
            Assert.AreEqual(l.Type, LinkStateAdvertisementType.ASExternal);
            Assert.AreEqual(l.Options, 0x02);

            l = lsas[6];
            Assert.AreEqual(l.Checksum, 0x3341);
            Assert.AreEqual(l.AdvertisingRouter, System.Net.IPAddress.Parse("192.168.170.2"));
            Assert.AreEqual(l.Length, 36);
            Assert.AreEqual(l.Id, System.Net.IPAddress.Parse("192.168.172.0"));
            Assert.AreEqual(l.Age, 2);
            Assert.AreEqual(l.SequenceNumber, 0x80000001);
            Assert.AreEqual(l.Type, LinkStateAdvertisementType.ASExternal);
            Assert.AreEqual(l.Options, 0x02);
        }

        [Test]
        public void TestLinkStateRequests()
        {
            OspfV2LinkStateRequestPacket p = (OspfV2LinkStateRequestPacket)lsrPacket;
            List<LinkStateRequest> requests = p.Requests;
            Assert.AreEqual(requests.Count, 7);

            LinkStateRequest r = requests[0];
            Assert.AreEqual(r.LSType, LinkStateAdvertisementType.Router);
            Assert.AreEqual(r.LinkStateID, System.Net.IPAddress.Parse("192.168.170.3"));
            Assert.AreEqual(r.AdvertisingRouter, System.Net.IPAddress.Parse("192.168.170.3"));

            r = requests[1];
            Assert.AreEqual(r.LSType, LinkStateAdvertisementType.ASExternal);
            Assert.AreEqual(r.LinkStateID, System.Net.IPAddress.Parse("80.212.16.0"));
            Assert.AreEqual(r.AdvertisingRouter, System.Net.IPAddress.Parse("192.168.170.2"));

            r = requests[2];
            Assert.AreEqual(r.LSType, LinkStateAdvertisementType.ASExternal);
            Assert.AreEqual(r.LinkStateID, System.Net.IPAddress.Parse("148.121.171.0"));
            Assert.AreEqual(r.AdvertisingRouter, System.Net.IPAddress.Parse("192.168.170.2"));

            r = requests[3];
            Assert.AreEqual(r.LSType, LinkStateAdvertisementType.ASExternal);
            Assert.AreEqual(r.LinkStateID, System.Net.IPAddress.Parse("192.130.120.0"));
            Assert.AreEqual(r.AdvertisingRouter, System.Net.IPAddress.Parse("192.168.170.2"));

            r = requests[4];
            Assert.AreEqual(r.LSType, LinkStateAdvertisementType.ASExternal);
            Assert.AreEqual(r.LinkStateID, System.Net.IPAddress.Parse("192.168.0.0"));
            Assert.AreEqual(r.AdvertisingRouter, System.Net.IPAddress.Parse("192.168.170.2"));

            r = requests[5];
            Assert.AreEqual(r.LSType, LinkStateAdvertisementType.ASExternal);
            Assert.AreEqual(r.LinkStateID, System.Net.IPAddress.Parse("192.168.1.0"));
            Assert.AreEqual(r.AdvertisingRouter, System.Net.IPAddress.Parse("192.168.170.2"));

            r = requests[6];
            Assert.AreEqual(r.LSType, LinkStateAdvertisementType.ASExternal);
            Assert.AreEqual(r.LinkStateID, System.Net.IPAddress.Parse("192.168.172.0"));
            Assert.AreEqual(r.AdvertisingRouter, System.Net.IPAddress.Parse("192.168.170.2"));
        }

        [Test]
        public void TestLSA()
        {
            RouterLinksAdvertisement rl;
            NetworkLinksAdvertisement nl;
            SummaryLinkAdvertisement sl;
            ASExternalLinkAdvertisement al;
            LinkStateAdvertisement l;

            Assert.AreNotEqual(null, lsaHolder);
            Assert.AreEqual(11, lsaHolder.LsaNumber);
            Assert.AreEqual(11, lsaHolder.Updates.Count);

            l = lsaHolder.Updates[0];
            Assert.AreEqual(typeof(RouterLinksAdvertisement), l.GetType());
            rl = (RouterLinksAdvertisement)l;
            Assert.AreEqual(446, rl.Age);
            Assert.AreEqual(0x22, rl.Options);
            Assert.AreEqual(LinkStateAdvertisementType.Router, rl.Type);
            Assert.AreEqual(System.Net.IPAddress.Parse("5.5.5.5"), rl.Id);
            Assert.AreEqual(System.Net.IPAddress.Parse("5.5.5.5"), rl.AdvertisingRouter);
            Assert.AreEqual(0x80000004, rl.SequenceNumber);
            Assert.AreEqual(0x7caa, rl.Checksum);
            Assert.AreEqual(48, rl.Length);
            Assert.AreEqual(0, rl.VBit);
            Assert.AreEqual(0, rl.EBit);
            Assert.AreEqual(0, rl.BBit);
            Assert.AreEqual(2, rl.RouterLinks.Count);

            RouterLink rlink = rl.RouterLinks[0];
            Assert.AreEqual(3, rlink.Type);
            Assert.AreEqual(System.Net.IPAddress.Parse("192.168.20.0"), rlink.LinkId);
            Assert.AreEqual(System.Net.IPAddress.Parse("255.255.255.0"), rlink.LinkData);
            Assert.AreEqual(0, rlink.TosNumber);
            Assert.AreEqual(10, rlink.Metric);

            rlink = rl.RouterLinks[1];
            Assert.AreEqual(2, rlink.Type);
            Assert.AreEqual(System.Net.IPAddress.Parse("10.0.20.2"), rlink.LinkId);
            Assert.AreEqual(System.Net.IPAddress.Parse("10.0.20.2"), rlink.LinkData);
            Assert.AreEqual(0, rlink.TosNumber);
            Assert.AreEqual(10, rlink.Metric);

            l = lsaHolder.Updates[1];
            Assert.AreEqual(typeof(RouterLinksAdvertisement), l.GetType());
            rl = (RouterLinksAdvertisement)l;
            Assert.AreEqual(10, rl.Age);
            Assert.AreEqual(0x22, rl.Options);
            Assert.AreEqual(LinkStateAdvertisementType.Router, rl.Type);
            Assert.AreEqual(System.Net.IPAddress.Parse("4.4.4.4"), rl.Id);
            Assert.AreEqual(System.Net.IPAddress.Parse("4.4.4.4"), rl.AdvertisingRouter);
            Assert.AreEqual(0x80000006, rl.SequenceNumber);
            Assert.AreEqual(0x36b1, rl.Checksum);
            Assert.AreEqual(36, rl.Length);
            Assert.AreEqual(0, rl.VBit);
            Assert.AreEqual(0, rl.EBit);
            Assert.AreEqual(1, rl.BBit);
            Assert.AreEqual(1, rl.RouterLinks.Count);

            rlink = rl.RouterLinks[0];
            Assert.AreEqual(3, rlink.Type);
            Assert.AreEqual(System.Net.IPAddress.Parse("10.0.20.0"), rlink.LinkId);
            Assert.AreEqual(System.Net.IPAddress.Parse("255.255.255.252"), rlink.LinkData);
            Assert.AreEqual(0, rlink.TosNumber);
            Assert.AreEqual(10, rlink.Metric);

            l = lsaHolder.Updates[2];
            Assert.AreEqual(typeof(NetworkLinksAdvertisement), l.GetType());
            nl = (NetworkLinksAdvertisement)l;
            Assert.AreEqual(446, nl.Age);
            Assert.AreEqual(0x22, nl.Options);
            Assert.AreEqual(LinkStateAdvertisementType.Network, nl.Type);
            Assert.AreEqual(System.Net.IPAddress.Parse("10.0.20.2"), nl.Id);
            Assert.AreEqual(System.Net.IPAddress.Parse("5.5.5.5"), nl.AdvertisingRouter);
            Assert.AreEqual(0x80000001, nl.SequenceNumber);
            Assert.AreEqual(0xf6ed, nl.Checksum);
            Assert.AreEqual(System.Net.IPAddress.Parse("255.255.255.252"), nl.NetworkMask);
            Assert.AreEqual(System.Net.IPAddress.Parse("5.5.5.5"), nl.AttachedRouters[0]);
            Assert.AreEqual(System.Net.IPAddress.Parse("4.4.4.4"), nl.AttachedRouters[1]);
            Assert.AreEqual(32, nl.Length);

            l = lsaHolder.Updates[3];
            Assert.AreEqual(typeof(SummaryLinkAdvertisement), l.GetType());
            sl = (SummaryLinkAdvertisement)l;
            Assert.AreEqual(11, sl.Age);
            Assert.AreEqual(0x22, sl.Options);
            Assert.AreEqual(LinkStateAdvertisementType.Summary, sl.Type);
            Assert.AreEqual(System.Net.IPAddress.Parse("192.168.10.0"), sl.Id);
            Assert.AreEqual(System.Net.IPAddress.Parse("4.4.4.4"), sl.AdvertisingRouter);
            Assert.AreEqual(0x80000001, sl.SequenceNumber);
            Assert.AreEqual(0x1e7d, sl.Checksum);
            Assert.AreEqual(System.Net.IPAddress.Parse("255.255.255.0"), sl.NetworkMask);
            Assert.AreEqual(30, sl.Metric);
            Assert.AreEqual(28, sl.Length);

            l = lsaHolder.Updates[4];
            Assert.AreEqual(typeof(SummaryLinkAdvertisement), l.GetType());
            sl = (SummaryLinkAdvertisement)l;
            Assert.AreEqual(11, sl.Age);
            Assert.AreEqual(0x22, sl.Options);
            Assert.AreEqual(LinkStateAdvertisementType.Summary, sl.Type);
            Assert.AreEqual(System.Net.IPAddress.Parse("10.0.10.0"), sl.Id);
            Assert.AreEqual(System.Net.IPAddress.Parse("4.4.4.4"), sl.AdvertisingRouter);
            Assert.AreEqual(0x80000001, sl.SequenceNumber);
            Assert.AreEqual(0xd631, sl.Checksum);
            Assert.AreEqual(System.Net.IPAddress.Parse("255.255.255.252"), sl.NetworkMask);
            Assert.AreEqual(20, sl.Metric);
            Assert.AreEqual(28, sl.Length);

            l = lsaHolder.Updates[6];
            Assert.AreEqual(typeof(SummaryLinkAdvertisement), l.GetType());
            sl = (SummaryLinkAdvertisement)l;
            Assert.AreEqual(11, sl.Age);
            Assert.AreEqual(0x22, sl.Options);
            Assert.AreEqual(LinkStateAdvertisementType.SummaryASBR, sl.Type);
            Assert.AreEqual(System.Net.IPAddress.Parse("2.2.2.2"), sl.Id);
            Assert.AreEqual(System.Net.IPAddress.Parse("4.4.4.4"), sl.AdvertisingRouter);
            Assert.AreEqual(0x80000001, sl.SequenceNumber);
            Assert.AreEqual(0x6fa0, sl.Checksum);
            Assert.AreEqual(System.Net.IPAddress.Parse("0.0.0.0"), sl.NetworkMask);
            Assert.AreEqual(20, sl.Metric);
            Assert.AreEqual(28, sl.Length);

            l = lsaHolder.Updates[7];
            Assert.AreEqual(typeof(ASExternalLinkAdvertisement), l.GetType());
            al = (ASExternalLinkAdvertisement)l;
            Assert.AreEqual(197, al.Age);
            Assert.AreEqual(0x20, al.Options);
            Assert.AreEqual(LinkStateAdvertisementType.ASExternal, al.Type);
            Assert.AreEqual(System.Net.IPAddress.Parse("172.16.3.0"), al.Id);
            Assert.AreEqual(System.Net.IPAddress.Parse("2.2.2.2"), al.AdvertisingRouter);
            Assert.AreEqual(0x80000001, al.SequenceNumber);
            Assert.AreEqual(0x2860, al.Checksum);
            Assert.AreEqual(System.Net.IPAddress.Parse("255.255.255.0"), al.NetworkMask);
            Assert.AreEqual(1, al.ASExternalLinks.Count);
            Assert.AreEqual(36, al.Length);

            ASExternalLink aslink = al.ASExternalLinks[0];
            Assert.AreEqual(aslink.EBit, 1);
            Assert.AreEqual(100, aslink.Metric);
            Assert.AreEqual(0, aslink.ExternalRouteTag);
            Assert.AreEqual(0, aslink.TypeOfService);
            Assert.AreEqual(System.Net.IPAddress.Parse("0.0.0.0"), aslink.ForwardingAddress);

            l = lsaHolder.Updates[8];
            Assert.AreEqual(typeof(ASExternalLinkAdvertisement), l.GetType());
            al = (ASExternalLinkAdvertisement)l;
            Assert.AreEqual(197, al.Age);
            Assert.AreEqual(0x20, al.Options);
            Assert.AreEqual(LinkStateAdvertisementType.ASExternal, al.Type);
            Assert.AreEqual(System.Net.IPAddress.Parse("172.16.2.0"), al.Id);
            Assert.AreEqual(System.Net.IPAddress.Parse("2.2.2.2"), al.AdvertisingRouter);
            Assert.AreEqual(0x80000001, al.SequenceNumber);
            Assert.AreEqual(0x3356, al.Checksum);
            Assert.AreEqual(System.Net.IPAddress.Parse("255.255.255.0"), al.NetworkMask);
            Assert.AreEqual(1, al.ASExternalLinks.Count);
            Assert.AreEqual(36, al.Length);

            aslink = al.ASExternalLinks[0];
            Assert.AreEqual(aslink.EBit, 1);
            Assert.AreEqual(100, aslink.Metric);
            Assert.AreEqual(0, aslink.ExternalRouteTag);
            Assert.AreEqual(0, aslink.TypeOfService);
            Assert.AreEqual(System.Net.IPAddress.Parse("0.0.0.0"), aslink.ForwardingAddress);
        }

        [Test]
        public void TestOSPFv2Auth()
        {
            RawCapture raw;
            var dev = new CaptureFileReaderDevice("../../CaptureFiles/ospfv2_md5.pcap");
            OspfV2HelloPacket[] testSubjects = new OspfV2HelloPacket[4];
            int i = 0;

            dev.Open();
            while ((raw = dev.GetNextPacket()) != null && i < 4)
            {
                testSubjects[i] = Packet.ParsePacket(raw.LinkLayerType, raw.Data).Extract<OspfV2HelloPacket>();
                i++;
            }
            dev.Close();

            Assert.AreEqual(testSubjects[0].Authentication, (long)0x000000103c7ec4f7);
            Assert.AreEqual(testSubjects[1].Authentication, (long)0x000000103c7ec4fc);
            Assert.AreEqual(testSubjects[2].Authentication, (long)0x000000103c7ec501);
            Assert.AreEqual(testSubjects[3].Authentication, (long)0x000000103c7ec505);
            Assert.AreEqual(0, testSubjects[0].NeighborIds.Count);
            Assert.AreEqual(0, testSubjects[1].NeighborIds.Count);
            Assert.AreEqual(1, testSubjects[2].NeighborIds.Count);
            Assert.AreEqual(1, testSubjects[3].NeighborIds.Count);
            Assert.AreEqual(System.Net.IPAddress.Parse("10.0.0.2"), testSubjects[2].NeighborIds[0]);
            Assert.AreEqual(System.Net.IPAddress.Parse("10.0.0.1"), testSubjects[3].NeighborIds[0]);
        }

        [Test]
        public void TestHelloConstruction()
        {
            //test ctor 1
            OspfV2HelloPacket p = new OspfV2HelloPacket(System.Net.IPAddress.Parse("255.255.255.0"), 2, 2);

            p.RouterId = System.Net.IPAddress.Parse("192.168.255.255");
            p.AreaId = System.Net.IPAddress.Parse("192.168.255.252");

            p.HelloOptions = 0x02;
            p.DesignatedRouterId = System.Net.IPAddress.Parse("192.168.1.1");
            p.BackupRouterId = System.Net.IPAddress.Parse("10.1.1.2");

            Assert.AreEqual(OspfVersion.OspfV2, p.Version);
            Assert.AreEqual(OspfPacketType.Hello, p.Type);
            Assert.AreEqual(0x02, p.HelloOptions);
            Assert.AreEqual(2, p.HelloInterval);
            Assert.AreEqual(2, p.RouterDeadInterval);
            Assert.AreEqual(System.Net.IPAddress.Parse("192.168.255.255"), p.RouterId);
            Assert.AreEqual(System.Net.IPAddress.Parse("192.168.255.252"), p.AreaId);
            Assert.AreEqual(System.Net.IPAddress.Parse("192.168.1.1"), p.DesignatedRouterId);
            Assert.AreEqual(System.Net.IPAddress.Parse("10.1.1.2"), p.BackupRouterId);
            Assert.AreEqual(System.Net.IPAddress.Parse("255.255.255.0"), p.NetworkMask);
            Assert.AreEqual(0, p.NeighborIds.Count);

            //test re-creation
            byte[] bytes = p.Bytes;
            OspfV2HelloPacket hp = new OspfV2HelloPacket(new ByteArraySegment(bytes));

            Assert.AreEqual(OspfVersion.OspfV2, hp.Version);
            Assert.AreEqual(OspfPacketType.Hello, hp.Type);
            Assert.AreEqual(0x02, p.HelloOptions);
            Assert.AreEqual(2, hp.HelloInterval);
            Assert.AreEqual(2, hp.RouterDeadInterval);
            Assert.AreEqual(System.Net.IPAddress.Parse("192.168.255.255"), hp.RouterId);
            Assert.AreEqual(System.Net.IPAddress.Parse("192.168.255.252"), hp.AreaId);
            Assert.AreEqual(System.Net.IPAddress.Parse("192.168.1.1"), hp.DesignatedRouterId);
            Assert.AreEqual(System.Net.IPAddress.Parse("10.1.1.2"), hp.BackupRouterId);
            Assert.AreEqual(System.Net.IPAddress.Parse("255.255.255.0"), hp.NetworkMask);
            Assert.AreEqual(0, hp.NeighborIds.Count);

            //test ctor 2
            List<System.Net.IPAddress> neighbors = new List<System.Net.IPAddress>();
            neighbors.Add(System.Net.IPAddress.Parse("172.168.144.1"));
            neighbors.Add(System.Net.IPAddress.Parse("123.192.133.255"));
            neighbors.Add(System.Net.IPAddress.Parse("61.72.84.3"));
            neighbors.Add(System.Net.IPAddress.Parse("127.0.0.1"));

            OspfV2HelloPacket p2 = new OspfV2HelloPacket(System.Net.IPAddress.Parse("255.255.255.0"), 3, 4, neighbors);
            Assert.AreEqual(3, p2.HelloInterval);
            Assert.AreEqual(4, p2.RouterDeadInterval);
            Assert.AreEqual(4, p2.NeighborIds.Count);
            Assert.AreEqual(System.Net.IPAddress.Parse("172.168.144.1"), p2.NeighborIds[0]);
            Assert.AreEqual(System.Net.IPAddress.Parse("123.192.133.255"), p2.NeighborIds[1]);
            Assert.AreEqual(System.Net.IPAddress.Parse("61.72.84.3"), p2.NeighborIds[2]);
            Assert.AreEqual(System.Net.IPAddress.Parse("127.0.0.1"), p2.NeighborIds[3]);
        }

        [Test]
        public void TestDDConstruction()
        {
            //test ctor 1
            OspfV2DatabaseDescriptorPacket d = new OspfV2DatabaseDescriptorPacket();
            d.InterfaceMtu = 1500;
            d.DDSequence = 1098361214;
            d.DescriptionOptions = 0x02;

            Assert.AreEqual(OspfPacketType.DatabaseDescription, d.Type);
            Assert.AreEqual(1098361214, d.DDSequence);
            Assert.AreEqual(1500, d.InterfaceMtu);
            Assert.AreEqual(0x02, d.DescriptionOptions);

            //test re-creation
            byte[] bytes = d.Bytes;
            OspfV2DatabaseDescriptorPacket dp = new OspfV2DatabaseDescriptorPacket(new ByteArraySegment(bytes));

            Assert.AreEqual(OspfPacketType.DatabaseDescription, d.Type);
            Assert.AreEqual(1098361214, dp.DDSequence);
            Assert.AreEqual(1500, dp.InterfaceMtu);
            Assert.AreEqual(0x02, dp.DescriptionOptions);

            //test ctor 2
            List<LinkStateAdvertisement> lsas = new List<LinkStateAdvertisement>();

            LinkStateAdvertisement l = new LinkStateAdvertisement();
            l.AdvertisingRouter = System.Net.IPAddress.Parse("192.168.170.3");
            l.Id = System.Net.IPAddress.Parse("192.168.170.3");
            l.Age = 1;
            l.SequenceNumber = 0x80000001;
            l.Type = LinkStateAdvertisementType.Router;
            l.Options = 0x02;
            lsas.Add(l);

            l = new LinkStateAdvertisement();
            l.AdvertisingRouter = System.Net.IPAddress.Parse("192.168.170.2");
            l.Id = System.Net.IPAddress.Parse("80.212.16.0");
            l.Age = 2;
            l.SequenceNumber = 0x80000001;
            l.Type = LinkStateAdvertisementType.ASExternal;
            l.Options = 0x02;
            lsas.Add(l);

            l = new LinkStateAdvertisement();
            l.AdvertisingRouter = System.Net.IPAddress.Parse("192.168.170.2");
            l.Id = System.Net.IPAddress.Parse("148.121.171.0");
            l.Age = 2;
            l.SequenceNumber = 0x80000001;
            l.Type = LinkStateAdvertisementType.ASExternal;
            l.Options = 0x02;
            lsas.Add(l);

            OspfV2DatabaseDescriptorPacket ddl = new OspfV2DatabaseDescriptorPacket(lsas);
            ddl.InterfaceMtu = 1400;
            ddl.DDSequence = 123456789;
            ddl.DescriptionOptions = 0x03;

            Assert.AreEqual(OspfPacketType.DatabaseDescription, ddl.Type);
            Assert.AreEqual(123456789, ddl.DDSequence);
            Assert.AreEqual(1400, ddl.InterfaceMtu);
            Assert.AreEqual(0x03, ddl.DescriptionOptions);

            Assert.AreEqual(3, ddl.Headers.Count);

            Assert.AreEqual(ddl.Headers[0].AdvertisingRouter, System.Net.IPAddress.Parse("192.168.170.3"));
            Assert.AreEqual(ddl.Headers[0].Id, System.Net.IPAddress.Parse("192.168.170.3"));
            Assert.AreEqual(ddl.Headers[0].Age, 1);
            Assert.AreEqual(ddl.Headers[0].SequenceNumber, 0x80000001);
            Assert.AreEqual(ddl.Headers[0].Type, LinkStateAdvertisementType.Router);
            Assert.AreEqual(ddl.Headers[0].Options, 0x02);

            Assert.AreEqual(ddl.Headers[1].AdvertisingRouter, System.Net.IPAddress.Parse("192.168.170.2"));
            Assert.AreEqual(ddl.Headers[1].Id, System.Net.IPAddress.Parse("80.212.16.0"));
            Assert.AreEqual(ddl.Headers[1].Age, 2);
            Assert.AreEqual(ddl.Headers[1].SequenceNumber, 0x80000001);
            Assert.AreEqual(ddl.Headers[1].Type, LinkStateAdvertisementType.ASExternal);
            Assert.AreEqual(ddl.Headers[1].Options, 0x02);

            Assert.AreEqual(ddl.Headers[2].AdvertisingRouter, System.Net.IPAddress.Parse("192.168.170.2"));
            Assert.AreEqual(ddl.Headers[2].Id, System.Net.IPAddress.Parse("148.121.171.0"));
            Assert.AreEqual(ddl.Headers[2].Age, 2);
            Assert.AreEqual(ddl.Headers[2].SequenceNumber, 0x80000001);
            Assert.AreEqual(ddl.Headers[2].Type, LinkStateAdvertisementType.ASExternal);
            Assert.AreEqual(ddl.Headers[2].Options, 0x02);
        }

        [Test]
        public void TestLSRConstruction()
        {
            //test ctor 1
            OspfV2LinkStateRequestPacket p = new OspfV2LinkStateRequestPacket();

            p.RouterId = System.Net.IPAddress.Parse("192.168.255.255");
            p.AreaId = System.Net.IPAddress.Parse("192.168.255.252");

            Assert.AreEqual(OspfVersion.OspfV2, p.Version);
            Assert.AreEqual(OspfPacketType.LinkStateRequest, p.Type);

            Assert.AreEqual(System.Net.IPAddress.Parse("192.168.255.255"), p.RouterId);
            Assert.AreEqual(System.Net.IPAddress.Parse("192.168.255.252"), p.AreaId);

            //test re-creation
            byte[] bytes = p.Bytes;
            OspfV2LinkStateRequestPacket lp = new OspfV2LinkStateRequestPacket(new ByteArraySegment(bytes));

            Assert.AreEqual(OspfVersion.OspfV2, lp.Version);
            Assert.AreEqual(OspfPacketType.LinkStateRequest, lp.Type);

            Assert.AreEqual(System.Net.IPAddress.Parse("192.168.255.255"), lp.RouterId);
            Assert.AreEqual(System.Net.IPAddress.Parse("192.168.255.252"), lp.AreaId);

            //test ctor 2
            List<LinkStateRequest> lsrs = new List<LinkStateRequest>();

            LinkStateRequest r = new LinkStateRequest();
            r.LSType = LinkStateAdvertisementType.Router;
            r.LinkStateID = System.Net.IPAddress.Parse("192.168.170.3");
            r.AdvertisingRouter = System.Net.IPAddress.Parse("192.168.170.3");
            lsrs.Add(r);

            r = new LinkStateRequest();
            r.LSType = LinkStateAdvertisementType.ASExternal;
            r.LinkStateID = System.Net.IPAddress.Parse("80.212.16.0");
            r.AdvertisingRouter = System.Net.IPAddress.Parse("192.168.170.2");
            lsrs.Add(r);

            r = new LinkStateRequest();
            r.LSType = LinkStateAdvertisementType.Network;
            r.LinkStateID = System.Net.IPAddress.Parse("148.121.171.0");
            r.AdvertisingRouter = System.Net.IPAddress.Parse("192.168.170.2");
            lsrs.Add(r);

            OspfV2LinkStateRequestPacket lp2 = new OspfV2LinkStateRequestPacket(lsrs);

            lp2.RouterId = System.Net.IPAddress.Parse("10.0.1.255");
            lp2.AreaId = System.Net.IPAddress.Parse("10.0.2.252");

            Assert.AreEqual(OspfVersion.OspfV2, lp2.Version);
            Assert.AreEqual(OspfPacketType.LinkStateRequest, lp2.Type);

            Assert.AreEqual(System.Net.IPAddress.Parse("10.0.1.255"), lp2.RouterId);
            Assert.AreEqual(System.Net.IPAddress.Parse("10.0.2.252"), lp2.AreaId);

            Assert.AreEqual(lp2.Requests[0].LSType, LinkStateAdvertisementType.Router);
            Assert.AreEqual(lp2.Requests[0].LinkStateID, System.Net.IPAddress.Parse("192.168.170.3"));
            Assert.AreEqual(lp2.Requests[0].AdvertisingRouter, System.Net.IPAddress.Parse("192.168.170.3"));

            Assert.AreEqual(lp2.Requests[1].LSType, LinkStateAdvertisementType.ASExternal);
            Assert.AreEqual(lp2.Requests[1].LinkStateID, System.Net.IPAddress.Parse("80.212.16.0"));
            Assert.AreEqual(lp2.Requests[1].AdvertisingRouter, System.Net.IPAddress.Parse("192.168.170.2"));

            Assert.AreEqual(lp2.Requests[2].LSType, LinkStateAdvertisementType.Network);
            Assert.AreEqual(lp2.Requests[2].LinkStateID, System.Net.IPAddress.Parse("148.121.171.0"));
            Assert.AreEqual(lp2.Requests[2].AdvertisingRouter, System.Net.IPAddress.Parse("192.168.170.2"));
        }

        [Test]
        public void TestRouterLSAConstruction()
        {
            //ctor 1
            RouterLinksAdvertisement rl = new RouterLinksAdvertisement();

            rl.Age = 333;
            rl.Options = 0x20;
            rl.Id = System.Net.IPAddress.Parse("1.1.1.1");
            rl.AdvertisingRouter = System.Net.IPAddress.Parse("2.2.2.2");
            rl.SequenceNumber = 0x80000001;
            rl.Checksum = 0xaaaa;
            rl.VBit = 1;
            rl.EBit = 0;
            rl.BBit = 1;


            Assert.AreEqual(333, rl.Age);
            Assert.AreEqual(0x20, rl.Options);
            Assert.AreEqual(LinkStateAdvertisementType.Router, rl.Type);
            Assert.AreEqual(System.Net.IPAddress.Parse("1.1.1.1"), rl.Id);
            Assert.AreEqual(System.Net.IPAddress.Parse("2.2.2.2"), rl.AdvertisingRouter);
            Assert.AreEqual(0x80000001, rl.SequenceNumber);
            Assert.AreEqual(0xaaaa, rl.Checksum);
            Assert.AreEqual(24, rl.Length);
            Assert.AreEqual(1, rl.VBit);
            Assert.AreEqual(0, rl.EBit);
            Assert.AreEqual(1, rl.BBit);
            Assert.AreEqual(0, rl.RouterLinks.Count);

            List<RouterLink> rlist = new List<RouterLink>();

            RouterLink rlink = new RouterLink();
            rlink.Type = 3;
            rlink.LinkId = System.Net.IPAddress.Parse("192.168.20.0");
            rlink.LinkData = System.Net.IPAddress.Parse("255.255.255.0");
            rlink.TosNumber = 0;
            rlink.Metric = 10;
            rlist.Add(rlink);

            rlink = new RouterLink();
            rlink.Type = 2;
            rlink.LinkId = System.Net.IPAddress.Parse("10.0.20.2");
            rlink.LinkData = System.Net.IPAddress.Parse("10.0.20.2");
            rlink.TosNumber = 0;
            rlink.Metric = 10;
            rlist.Add(rlink);

            //ctor 2
            rl = new RouterLinksAdvertisement(rlist);

            rl.Age = 446;
            rl.Options = 0x22;
            rl.Id = System.Net.IPAddress.Parse("5.5.5.5");
            rl.AdvertisingRouter = System.Net.IPAddress.Parse("5.5.5.5");
            rl.SequenceNumber = 0x80000004;
            rl.Checksum = 0x7caa;
            rl.VBit = 0;
            rl.EBit = 0;
            rl.BBit = 0;

            Assert.AreEqual(446, rl.Age);
            Assert.AreEqual(0x22, rl.Options);
            Assert.AreEqual(LinkStateAdvertisementType.Router, rl.Type);
            Assert.AreEqual(System.Net.IPAddress.Parse("5.5.5.5"), rl.Id);
            Assert.AreEqual(System.Net.IPAddress.Parse("5.5.5.5"), rl.AdvertisingRouter);
            Assert.AreEqual(0x80000004, rl.SequenceNumber);
            Assert.AreEqual(0x7caa, rl.Checksum);
            Assert.AreEqual(48, rl.Length);
            Assert.AreEqual(0, rl.VBit);
            Assert.AreEqual(0, rl.EBit);
            Assert.AreEqual(0, rl.BBit);
            Assert.AreEqual(2, rl.RouterLinks.Count);

            rlink = rl.RouterLinks[0];
            Assert.AreEqual(3, rlink.Type);
            Assert.AreEqual(System.Net.IPAddress.Parse("192.168.20.0"), rlink.LinkId);
            Assert.AreEqual(System.Net.IPAddress.Parse("255.255.255.0"), rlink.LinkData);
            Assert.AreEqual(0, rlink.TosNumber);
            Assert.AreEqual(10, rlink.Metric);

            rlink = rl.RouterLinks[1];
            Assert.AreEqual(2, rlink.Type);
            Assert.AreEqual(System.Net.IPAddress.Parse("10.0.20.2"), rlink.LinkId);
            Assert.AreEqual(System.Net.IPAddress.Parse("10.0.20.2"), rlink.LinkData);
            Assert.AreEqual(0, rlink.TosNumber);
            Assert.AreEqual(10, rlink.Metric);

            //re-creation
        }

        [Test]
        public void TestNetworkLSACreation()
        {
            //ctor 1
            NetworkLinksAdvertisement nl = new NetworkLinksAdvertisement();

            nl.Age = 333;
            nl.Options = 0x20;
            nl.Type = LinkStateAdvertisementType.Network;
            nl.Id = System.Net.IPAddress.Parse("1.1.1.1");
            nl.AdvertisingRouter = System.Net.IPAddress.Parse("2.2.2.2");
            nl.SequenceNumber = 0x8000000F;
            nl.Checksum = 0xdede;
            nl.NetworkMask = System.Net.IPAddress.Parse("255.255.255.252");

            Assert.AreEqual(333, nl.Age);
            Assert.AreEqual(0x20, nl.Options);
            Assert.AreEqual(LinkStateAdvertisementType.Network, nl.Type);
            Assert.AreEqual(System.Net.IPAddress.Parse("1.1.1.1"), nl.Id);
            Assert.AreEqual(System.Net.IPAddress.Parse("2.2.2.2"), nl.AdvertisingRouter);
            Assert.AreEqual(0x8000000F, nl.SequenceNumber);
            Assert.AreEqual(0xdede, nl.Checksum);
            Assert.AreEqual(System.Net.IPAddress.Parse("255.255.255.252"), nl.NetworkMask);
            Assert.AreEqual(24, nl.Length);

            //ctor 2
            List<System.Net.IPAddress> rtrs = new List<System.Net.IPAddress>();
            rtrs.Add(System.Net.IPAddress.Parse("5.5.5.5"));
            rtrs.Add(System.Net.IPAddress.Parse("4.4.4.4"));

            nl = new NetworkLinksAdvertisement(rtrs);

            nl.Age = 446;
            nl.Options = 0x22;
            nl.Type = LinkStateAdvertisementType.Network;
            nl.Id = System.Net.IPAddress.Parse("10.0.20.2");
            nl.AdvertisingRouter = System.Net.IPAddress.Parse("5.5.5.5");
            nl.SequenceNumber = 0x80000001;
            nl.Checksum = 0xf6ed;
            nl.NetworkMask = System.Net.IPAddress.Parse("255.255.255.252");

            Assert.AreEqual(446, nl.Age);
            Assert.AreEqual(0x22, nl.Options);
            Assert.AreEqual(LinkStateAdvertisementType.Network, nl.Type);
            Assert.AreEqual(System.Net.IPAddress.Parse("10.0.20.2"), nl.Id);
            Assert.AreEqual(System.Net.IPAddress.Parse("5.5.5.5"), nl.AdvertisingRouter);
            Assert.AreEqual(0x80000001, nl.SequenceNumber);
            Assert.AreEqual(0xf6ed, nl.Checksum);
            Assert.AreEqual(System.Net.IPAddress.Parse("255.255.255.252"), nl.NetworkMask);
            Assert.AreEqual(System.Net.IPAddress.Parse("5.5.5.5"), nl.AttachedRouters[0]);
            Assert.AreEqual(System.Net.IPAddress.Parse("4.4.4.4"), nl.AttachedRouters[1]);
            Assert.AreEqual(32, nl.Length);
        }

        [Test]
        public void TestSummaryLSAConstruction()
        {
            //ctor 1
            SummaryLinkAdvertisement sl = new SummaryLinkAdvertisement();

            sl.Age = 22;
            sl.Options = 0x20;
            sl.Type = LinkStateAdvertisementType.Summary;
            sl.Id = System.Net.IPAddress.Parse("1.1.1.1");
            sl.AdvertisingRouter = System.Net.IPAddress.Parse("4.4.4.4");
            sl.SequenceNumber = 0x8000000F;
            sl.Checksum = 0xdddd;
            sl.NetworkMask = System.Net.IPAddress.Parse("255.255.255.0");
            sl.Metric = 10;

            Assert.AreEqual(22, sl.Age);
            Assert.AreEqual(0x20, sl.Options);
            Assert.AreEqual(LinkStateAdvertisementType.Summary, sl.Type);
            Assert.AreEqual(System.Net.IPAddress.Parse("1.1.1.1"), sl.Id);
            Assert.AreEqual(System.Net.IPAddress.Parse("4.4.4.4"), sl.AdvertisingRouter);
            Assert.AreEqual(0x8000000F, sl.SequenceNumber);
            Assert.AreEqual(0xdddd, sl.Checksum);
            Assert.AreEqual(System.Net.IPAddress.Parse("255.255.255.0"), sl.NetworkMask);
            Assert.AreEqual(10, sl.Metric);
            Assert.AreEqual(28, sl.Length);
            Assert.AreEqual(0, sl.TosMetrics.Count);

            List<TypeOfServiceMetric> tms = new List<TypeOfServiceMetric>();

            TypeOfServiceMetric tm = new TypeOfServiceMetric();
            tm.TypeOfService = 1;
            tm.Metric = 11;
            tms.Add(tm);

            tm = new TypeOfServiceMetric();
            tm.TypeOfService = 2;
            tm.Metric = 22;
            tms.Add(tm);

            //ctor 2
            sl = new SummaryLinkAdvertisement(tms);

            sl.Age = 11;
            sl.Options = 0x22;
            sl.Type = LinkStateAdvertisementType.Summary;
            sl.Id = System.Net.IPAddress.Parse("192.168.10.0");
            sl.AdvertisingRouter = System.Net.IPAddress.Parse("4.4.4.4");
            sl.SequenceNumber = 0x80000001;
            sl.Checksum = 0x1e7d;
            sl.NetworkMask = System.Net.IPAddress.Parse("255.255.255.0");
            sl.Metric = 30;

            Assert.AreEqual(11, sl.Age);
            Assert.AreEqual(0x22, sl.Options);
            Assert.AreEqual(LinkStateAdvertisementType.Summary, sl.Type);
            Assert.AreEqual(System.Net.IPAddress.Parse("192.168.10.0"), sl.Id);
            Assert.AreEqual(System.Net.IPAddress.Parse("4.4.4.4"), sl.AdvertisingRouter);
            Assert.AreEqual(0x80000001, sl.SequenceNumber);
            Assert.AreEqual(0x1e7d, sl.Checksum);
            Assert.AreEqual(System.Net.IPAddress.Parse("255.255.255.0"), sl.NetworkMask);
            Assert.AreEqual(30, sl.Metric);
            Assert.AreEqual(36, sl.Length);
            Assert.AreEqual(2, sl.TosMetrics.Count);

            Assert.AreEqual(1, sl.TosMetrics[0].TypeOfService);
            Assert.AreEqual(11, sl.TosMetrics[0].Metric);
            Assert.AreEqual(2, sl.TosMetrics[1].TypeOfService);
            Assert.AreEqual(22, sl.TosMetrics[1].Metric);
        }

        [Test]
        public void TestASExternalLSAConstruction()
        {
            //ctor 1
            ASExternalLinkAdvertisement al = new ASExternalLinkAdvertisement();

            al.Age = 90;
            al.Options = 0x22;
            al.Type = LinkStateAdvertisementType.ASExternal;
            al.Id = System.Net.IPAddress.Parse("1.1.1.1");
            al.AdvertisingRouter = System.Net.IPAddress.Parse("3.3.3.3");
            al.SequenceNumber = 0x8000000F;
            al.Checksum = 0x3333;
            al.NetworkMask = System.Net.IPAddress.Parse("255.255.255.252");

            Assert.AreEqual(90, al.Age);
            Assert.AreEqual(0x22, al.Options);
            Assert.AreEqual(LinkStateAdvertisementType.ASExternal, al.Type);
            Assert.AreEqual(System.Net.IPAddress.Parse("1.1.1.1"), al.Id);
            Assert.AreEqual(System.Net.IPAddress.Parse("3.3.3.3"), al.AdvertisingRouter);
            Assert.AreEqual(0x8000000F, al.SequenceNumber);
            Assert.AreEqual(0x3333, al.Checksum);
            Assert.AreEqual(System.Net.IPAddress.Parse("255.255.255.252"), al.NetworkMask);
            Assert.AreEqual(0, al.ASExternalLinks.Count);
            Assert.AreEqual(24, al.Length);


            //ctor 2;
            ASExternalLink aslink = new ASExternalLink();
            aslink.EBit = 1;
            aslink.Metric = 100;
            aslink.ExternalRouteTag = 0;
            aslink.TypeOfService = 0;
            aslink.ForwardingAddress = System.Net.IPAddress.Parse("0.0.0.0");

            List<ASExternalLink> links = new List<ASExternalLink>();
            links.Add(aslink);

            al = new ASExternalLinkAdvertisement(links);

            al.Age = 197;
            al.Options = 0x20;
            al.Type = LinkStateAdvertisementType.ASExternal;
            al.Id = System.Net.IPAddress.Parse("172.16.2.0");
            al.AdvertisingRouter = System.Net.IPAddress.Parse("2.2.2.2");
            al.SequenceNumber = 0x80000001;
            al.Checksum = 0x3356;
            al.NetworkMask = System.Net.IPAddress.Parse("255.255.255.0");


            Assert.AreEqual(197, al.Age);
            Assert.AreEqual(0x20, al.Options);
            Assert.AreEqual(LinkStateAdvertisementType.ASExternal, al.Type);
            Assert.AreEqual(System.Net.IPAddress.Parse("172.16.2.0"), al.Id);
            Assert.AreEqual(System.Net.IPAddress.Parse("2.2.2.2"), al.AdvertisingRouter);
            Assert.AreEqual(0x80000001, al.SequenceNumber);
            Assert.AreEqual(0x3356, al.Checksum);
            Assert.AreEqual(System.Net.IPAddress.Parse("255.255.255.0"), al.NetworkMask);
            Assert.AreEqual(1, al.ASExternalLinks.Count);
            Assert.AreEqual(36, al.Length);

            aslink = al.ASExternalLinks[0];
            Assert.AreEqual(1, aslink.EBit);
            Assert.AreEqual(100, aslink.Metric);
            Assert.AreEqual(0, aslink.ExternalRouteTag);
            Assert.AreEqual(0, aslink.TypeOfService);
            Assert.AreEqual(System.Net.IPAddress.Parse("0.0.0.0"), aslink.ForwardingAddress);
        }

        [Test]
        public void TestLSUConstruction()
        {
            RouterLinksAdvertisement rl;
            NetworkLinksAdvertisement nl;
            SummaryLinkAdvertisement sl;
            ASExternalLinkAdvertisement al;

            //test ctor 1
            OspfV2LinkStateUpdatePacket p = new OspfV2LinkStateUpdatePacket();

            p.RouterId = System.Net.IPAddress.Parse("192.168.255.255");
            p.AreaId = System.Net.IPAddress.Parse("192.168.255.252");

            Assert.AreEqual(OspfVersion.OspfV2, p.Version);
            Assert.AreEqual(OspfPacketType.LinkStateUpdate, p.Type);

            Assert.AreEqual(System.Net.IPAddress.Parse("192.168.255.255"), p.RouterId);
            Assert.AreEqual(System.Net.IPAddress.Parse("192.168.255.252"), p.AreaId);

            Assert.AreEqual(0, p.LsaNumber);

            //test re-creation
            byte[] bytes = p.Bytes;
            OspfV2LinkStateUpdatePacket lp = new OspfV2LinkStateUpdatePacket(new ByteArraySegment(bytes));

            Assert.AreEqual(OspfVersion.OspfV2, lp.Version);
            Assert.AreEqual(OspfPacketType.LinkStateUpdate, lp.Type);

            Assert.AreEqual(System.Net.IPAddress.Parse("192.168.255.255"), lp.RouterId);
            Assert.AreEqual(System.Net.IPAddress.Parse("192.168.255.252"), lp.AreaId);
            Assert.AreEqual(0, p.LsaNumber);

            //ctor 2
            //add routerer LSA
            List<RouterLink> rlist = new List<RouterLink>();

            RouterLink rlink = new RouterLink();
            rlink.Type = 3;
            rlink.LinkId = System.Net.IPAddress.Parse("192.168.20.0");
            rlink.LinkData = System.Net.IPAddress.Parse("255.255.255.0");
            rlink.TosNumber = 0;
            rlink.Metric = 10;
            rlist.Add(rlink);

            rlink = new RouterLink();
            rlink.Type = 2;
            rlink.LinkId = System.Net.IPAddress.Parse("10.0.20.2");
            rlink.LinkData = System.Net.IPAddress.Parse("10.0.20.2");
            rlink.TosNumber = 0;
            rlink.Metric = 10;
            rlist.Add(rlink);
            rl = new RouterLinksAdvertisement(rlist);

            rl.Age = 446;
            rl.Options = 0x22;
            rl.Id = System.Net.IPAddress.Parse("5.5.5.5");
            rl.AdvertisingRouter = System.Net.IPAddress.Parse("5.5.5.5");
            rl.SequenceNumber = 0x80000004;
            rl.Checksum = 0x7caa;
            rl.VBit = 0;
            rl.EBit = 0;
            rl.BBit = 0;

            //add network lsa
            List<System.Net.IPAddress> rtrs = new List<System.Net.IPAddress>();
            rtrs.Add(System.Net.IPAddress.Parse("5.5.5.5"));
            rtrs.Add(System.Net.IPAddress.Parse("4.4.4.4"));

            nl = new NetworkLinksAdvertisement(rtrs);

            nl.Age = 446;
            nl.Options = 0x22;
            nl.Type = LinkStateAdvertisementType.Network;
            nl.Id = System.Net.IPAddress.Parse("10.0.20.2");
            nl.AdvertisingRouter = System.Net.IPAddress.Parse("5.5.5.5");
            nl.SequenceNumber = 0x80000001;
            nl.Checksum = 0xf6ed;
            nl.NetworkMask = System.Net.IPAddress.Parse("255.255.255.252");

            //add summary lsa
            List<TypeOfServiceMetric> tms = new List<TypeOfServiceMetric>();

            TypeOfServiceMetric tm = new TypeOfServiceMetric();
            tm.TypeOfService = 1;
            tm.Metric = 11;
            tms.Add(tm);

            tm = new TypeOfServiceMetric();
            tm.TypeOfService = 2;
            tm.Metric = 22;
            tms.Add(tm);

            sl = new SummaryLinkAdvertisement(tms);

            sl.Age = 11;
            sl.Options = 0x22;
            sl.Type = LinkStateAdvertisementType.Summary;
            sl.Id = System.Net.IPAddress.Parse("192.168.10.0");
            sl.AdvertisingRouter = System.Net.IPAddress.Parse("4.4.4.4");
            sl.SequenceNumber = 0x80000001;
            sl.Checksum = 0x1e7d;
            sl.NetworkMask = System.Net.IPAddress.Parse("255.255.255.0");
            sl.Metric = 30;

            //add AS External LSA
            ASExternalLink aslink = new ASExternalLink();
            aslink.EBit = 1;
            aslink.Metric = 100;
            aslink.ExternalRouteTag = 0;
            aslink.TypeOfService = 0;
            aslink.ForwardingAddress = System.Net.IPAddress.Parse("0.0.0.0");

            List<ASExternalLink> links = new List<ASExternalLink>();
            links.Add(aslink);

            al = new ASExternalLinkAdvertisement(links);

            al.Age = 197;
            al.Options = 0x20;
            al.Type = LinkStateAdvertisementType.ASExternal;
            al.Id = System.Net.IPAddress.Parse("172.16.2.0");
            al.AdvertisingRouter = System.Net.IPAddress.Parse("2.2.2.2");
            al.SequenceNumber = 0x80000001;
            al.Checksum = 0x3356;
            al.NetworkMask = System.Net.IPAddress.Parse("255.255.255.0");

            //put them all together in a list
            List<LinkStateAdvertisement> lsas = new List<LinkStateAdvertisement>();
            lsas.Add(rl);
            lsas.Add(nl);
            lsas.Add(sl);
            lsas.Add(al);

            p = new OspfV2LinkStateUpdatePacket(lsas);

            p.RouterId = System.Net.IPAddress.Parse("192.168.255.255");
            p.AreaId = System.Net.IPAddress.Parse("192.168.255.252");

            Assert.AreEqual(OspfVersion.OspfV2, p.Version);
            Assert.AreEqual(OspfPacketType.LinkStateUpdate, p.Type);

            Assert.AreEqual(System.Net.IPAddress.Parse("192.168.255.255"), p.RouterId);
            Assert.AreEqual(System.Net.IPAddress.Parse("192.168.255.252"), p.AreaId);

            Assert.AreEqual(4, p.LsaNumber);


            //test router lsa
            rl = (RouterLinksAdvertisement)p.Updates[0];
            Assert.AreEqual(446, rl.Age);
            Assert.AreEqual(0x22, rl.Options);
            Assert.AreEqual(LinkStateAdvertisementType.Router, rl.Type);
            Assert.AreEqual(System.Net.IPAddress.Parse("5.5.5.5"), rl.Id);
            Assert.AreEqual(System.Net.IPAddress.Parse("5.5.5.5"), rl.AdvertisingRouter);
            Assert.AreEqual(0x80000004, rl.SequenceNumber);
            Assert.AreEqual(0x7caa, rl.Checksum);
            Assert.AreEqual(48, rl.Length);
            Assert.AreEqual(0, rl.VBit);
            Assert.AreEqual(0, rl.EBit);
            Assert.AreEqual(0, rl.BBit);
            Assert.AreEqual(2, rl.RouterLinks.Count);

            rlink = rl.RouterLinks[0];
            Assert.AreEqual(3, rlink.Type);
            Assert.AreEqual(System.Net.IPAddress.Parse("192.168.20.0"), rlink.LinkId);
            Assert.AreEqual(System.Net.IPAddress.Parse("255.255.255.0"), rlink.LinkData);
            Assert.AreEqual(0, rlink.TosNumber);
            Assert.AreEqual(10, rlink.Metric);

            rlink = rl.RouterLinks[1];
            Assert.AreEqual(2, rlink.Type);
            Assert.AreEqual(System.Net.IPAddress.Parse("10.0.20.2"), rlink.LinkId);
            Assert.AreEqual(System.Net.IPAddress.Parse("10.0.20.2"), rlink.LinkData);
            Assert.AreEqual(0, rlink.TosNumber);
            Assert.AreEqual(10, rlink.Metric);

            //test network lsa
            nl = (NetworkLinksAdvertisement)p.Updates[1];
            Assert.AreEqual(446, nl.Age);
            Assert.AreEqual(0x22, nl.Options);
            Assert.AreEqual(LinkStateAdvertisementType.Network, nl.Type);
            Assert.AreEqual(System.Net.IPAddress.Parse("10.0.20.2"), nl.Id);
            Assert.AreEqual(System.Net.IPAddress.Parse("5.5.5.5"), nl.AdvertisingRouter);
            Assert.AreEqual(0x80000001, nl.SequenceNumber);
            Assert.AreEqual(0xf6ed, nl.Checksum);
            Assert.AreEqual(System.Net.IPAddress.Parse("255.255.255.252"), nl.NetworkMask);
            Assert.AreEqual(System.Net.IPAddress.Parse("5.5.5.5"), nl.AttachedRouters[0]);
            Assert.AreEqual(System.Net.IPAddress.Parse("4.4.4.4"), nl.AttachedRouters[1]);
            Assert.AreEqual(32, nl.Length);

            //test summary lsa
            sl = (SummaryLinkAdvertisement)p.Updates[2];
            Assert.AreEqual(11, sl.Age);
            Assert.AreEqual(0x22, sl.Options);
            Assert.AreEqual(LinkStateAdvertisementType.Summary, sl.Type);
            Assert.AreEqual(System.Net.IPAddress.Parse("192.168.10.0"), sl.Id);
            Assert.AreEqual(System.Net.IPAddress.Parse("4.4.4.4"), sl.AdvertisingRouter);
            Assert.AreEqual(0x80000001, sl.SequenceNumber);
            Assert.AreEqual(0x1e7d, sl.Checksum);
            Assert.AreEqual(System.Net.IPAddress.Parse("255.255.255.0"), sl.NetworkMask);
            Assert.AreEqual(30, sl.Metric);
            Assert.AreEqual(36, sl.Length);
            Assert.AreEqual(2, sl.TosMetrics.Count);

            Assert.AreEqual(1, sl.TosMetrics[0].TypeOfService);
            Assert.AreEqual(11, sl.TosMetrics[0].Metric);
            Assert.AreEqual(2, sl.TosMetrics[1].TypeOfService);
            Assert.AreEqual(22, sl.TosMetrics[1].Metric);

            //test AS-External-LSA
            al = (ASExternalLinkAdvertisement)p.Updates[3];
            Assert.AreEqual(197, al.Age);
            Assert.AreEqual(0x20, al.Options);
            Assert.AreEqual(LinkStateAdvertisementType.ASExternal, al.Type);
            Assert.AreEqual(System.Net.IPAddress.Parse("172.16.2.0"), al.Id);
            Assert.AreEqual(System.Net.IPAddress.Parse("2.2.2.2"), al.AdvertisingRouter);
            Assert.AreEqual(0x80000001, al.SequenceNumber);
            Assert.AreEqual(0x3356, al.Checksum);
            Assert.AreEqual(System.Net.IPAddress.Parse("255.255.255.0"), al.NetworkMask);
            Assert.AreEqual(1, al.ASExternalLinks.Count);
            Assert.AreEqual(36, al.Length);

            aslink = al.ASExternalLinks[0];
            Assert.AreEqual(1, aslink.EBit);
            Assert.AreEqual(100, aslink.Metric);
            Assert.AreEqual(0, aslink.ExternalRouteTag);
            Assert.AreEqual(0, aslink.TypeOfService);
            Assert.AreEqual(System.Net.IPAddress.Parse("0.0.0.0"), aslink.ForwardingAddress);
        }

        [Test]
        public void TestLSAPacketConstruction()
        {
            //test ctor 1
            OspfV2LinkStateAcknowledgmentPacket p = new OspfV2LinkStateAcknowledgmentPacket();

            Assert.AreEqual(OspfPacketType.LinkStateAcknowledgment, p.Type);

            //test re-creation
            byte[] bytes = p.Bytes;
            OspfV2DatabaseDescriptorPacket p2 = new OspfV2DatabaseDescriptorPacket(new ByteArraySegment(bytes));

            Assert.AreEqual(OspfPacketType.LinkStateAcknowledgment, p2.Type);

            //test ctor 2
            List<LinkStateAdvertisement> lsas = new List<LinkStateAdvertisement>();
            LinkStateAdvertisement l = new LinkStateAdvertisement();

            l.AdvertisingRouter = System.Net.IPAddress.Parse("192.168.170.3");
            l.Id = System.Net.IPAddress.Parse("192.168.170.3");
            l.Age = 1;
            l.SequenceNumber = 0x80000001;
            l.Type = LinkStateAdvertisementType.Router;
            l.Options = 0x02;
            lsas.Add(l);

            l = new LinkStateAdvertisement();
            l.AdvertisingRouter = System.Net.IPAddress.Parse("192.168.170.2");
            l.Id = System.Net.IPAddress.Parse("80.212.16.0");
            l.Age = 2;
            l.SequenceNumber = 0x80000001;
            l.Type = LinkStateAdvertisementType.ASExternal;
            l.Options = 0x02;
            lsas.Add(l);

            l = new LinkStateAdvertisement();
            l.AdvertisingRouter = System.Net.IPAddress.Parse("192.168.170.2");
            l.Id = System.Net.IPAddress.Parse("148.121.171.0");
            l.Age = 2;
            l.SequenceNumber = 0x80000001;
            l.Type = LinkStateAdvertisementType.ASExternal;
            l.Options = 0x02;
            lsas.Add(l);

            OspfV2LinkStateAcknowledgmentPacket p3 = new OspfV2LinkStateAcknowledgmentPacket(lsas);
            Assert.AreEqual(OspfPacketType.LinkStateAcknowledgment, p3.Type);

            Assert.AreEqual(3, p3.Acknowledgments.Count);

            Assert.AreEqual(p3.Acknowledgments[0].AdvertisingRouter, System.Net.IPAddress.Parse("192.168.170.3"));
            Assert.AreEqual(p3.Acknowledgments[0].Id, System.Net.IPAddress.Parse("192.168.170.3"));
            Assert.AreEqual(p3.Acknowledgments[0].Age, 1);
            Assert.AreEqual(p3.Acknowledgments[0].SequenceNumber, 0x80000001);
            Assert.AreEqual(p3.Acknowledgments[0].Type, LinkStateAdvertisementType.Router);
            Assert.AreEqual(p3.Acknowledgments[0].Options, 0x02);

            Assert.AreEqual(p3.Acknowledgments[1].AdvertisingRouter, System.Net.IPAddress.Parse("192.168.170.2"));
            Assert.AreEqual(p3.Acknowledgments[1].Id, System.Net.IPAddress.Parse("80.212.16.0"));
            Assert.AreEqual(p3.Acknowledgments[1].Age, 2);
            Assert.AreEqual(p3.Acknowledgments[1].SequenceNumber, 0x80000001);
            Assert.AreEqual(p3.Acknowledgments[1].Type, LinkStateAdvertisementType.ASExternal);
            Assert.AreEqual(p3.Acknowledgments[1].Options, 0x02);

            Assert.AreEqual(p3.Acknowledgments[2].AdvertisingRouter, System.Net.IPAddress.Parse("192.168.170.2"));
            Assert.AreEqual(p3.Acknowledgments[2].Id, System.Net.IPAddress.Parse("148.121.171.0"));
            Assert.AreEqual(p3.Acknowledgments[2].Age, 2);
            Assert.AreEqual(p3.Acknowledgments[2].SequenceNumber, 0x80000001);
            Assert.AreEqual(p3.Acknowledgments[2].Type, LinkStateAdvertisementType.ASExternal);
            Assert.AreEqual(p3.Acknowledgments[2].Options, 0x02);
        }

    }
}
