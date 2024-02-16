﻿
// Copyright (C) 2024 Maxim [maxirmx] Samsonov (www.sw.consulting)
// All rights reserved.
// This file is a part of dkg applcation
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions
// are met:
// 1. Redistributions of source code must retain the above copyright
// notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
// notice, this list of conditions and the following disclaimer in the
// documentation and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED
// TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
// PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDERS OR CONTRIBUTORS
// BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.

namespace dkg
{
    // Deal encapsulates the verifiable secret share and is sent by the dealer to a verifier.
    public class Deal : IMarshalling, IEquatable<Deal>
    {
        // Unique session identifier for this protocol run
        public byte[] SessionId { get; set; }
        // Private share generated by the dealer
        public PriShare SecShare { get; set; }
        // Threshold used for this secret sharing run
        public int T { get; set; }
        // Commitments are the coefficients used to verify the shares against
        public IPoint[] Commitments { get; set; }

        public Deal(byte[] sid, PriShare secShare, IPoint[] commitments, int t)
        {
            SessionId = sid;
            SecShare = secShare;
            T = t;
            Commitments = commitments;
        }

        public Deal()
        {
            SessionId = [];
            Commitments = [];
            SecShare = new(1, Suite.G.Scalar());
        }

        public byte[] GetBytes()
        {
            MemoryStream stream = new();
            MarshalBinary(stream);
            return stream.ToArray();
        }

        public void MarshalBinary(Stream s)
        {
            BinaryWriter bw = new(s);
            bw.Write(SessionId.Length);
            s.Write(SessionId, 0, SessionId.Length);
            SecShare.MarshalBinary(s);
            bw.Write(T);
            bw.Write(Commitments.Length);
            for (int i = 0; i < Commitments.Length; i++)
            {
                Commitments[i].MarshalBinary(s);
            }
        }

        public void UnmarshalBinary(Stream s)
        {
            BinaryReader bw = new(s);
            int l = bw.ReadInt32();
            SessionId = new byte[l];
            s.Read(SessionId, 0, SessionId.Length);
            SecShare.UnmarshalBinary(s);
            T = bw.ReadInt32();
            l = bw.ReadInt32();
            Commitments = new Secp256k1Point[l];
            for (int i = 0; i < Commitments.Length; i++)
            {
                Commitments[i] = new Secp256k1Point();
                Commitments[i].UnmarshalBinary(s);
            }
        }

        public int MarshalSize()
        {
            return 3 * sizeof(int) + SessionId.Length + SecShare.MarshalSize() +
                    Commitments.Length * Commitments[0].MarshalSize();
        }

        public bool Equals(Deal? other)
        {
            if (other == null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (SessionId.SequenceEqual(other.SessionId) &&
                SecShare.Equals(other.SecShare) &&
                T == other.T)
            {
                if (Commitments.Length != other.Commitments.Length)
                    return false;

                for (int i = 0; i < Commitments.Length; i++)
                {
                    if (!Commitments[i].Equals(other.Commitments[i]))
                        return false;
                }

                return true;
            }
            return false;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Deal);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + SessionId.GetHashCode();
                hash = hash * 23 + SecShare.GetHashCode();
                hash = hash * 23 + T.GetHashCode();
                for (int i = 0; i < Commitments.Length; i++)
                {
                    hash = hash * 23 + Commitments[i].GetHashCode();
                }
                return hash;
            }
        }
    }

    // EncryptedDeal contains the deal in a encrypted form only decipherable by the
    // correct recipient. The encryption is performed in a similar manner as what is
    // done in TLS. The dealer generates a temporary key pair, signs it with its
    // longterm secret key.
    public class EncryptedDeal(byte[] dhKey, byte[] signatire, byte[] nounce, byte[] cipher, byte[] tag)
    {
        // Ephemeral Diffie Hellman key
        public byte[] DHKey { get; set; } = dhKey;

        // Signature of the DH key by the longterm key of the dealer
        public byte[] Signature { get; set; } = signatire;

        // Nonce used for the encryption
        public byte[] Nonce { get; set; } = nounce;

        // AEAD encryption of the marshalled deal 
        public byte[] Cipher { get; set; } = cipher;
        public byte[] Tag { get; set; } = tag;
    }
}